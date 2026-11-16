classdef HVACWindowsBridge < handle
    % HVACWindowsBridge - Windows TCP <-> MATLAB ROS 2 adapter
    % Intended for MATLAB R2022b with ROS Toolbox and tcpserver support.
    % One local TCP client (Unity), JSON messages terminated by LF.
    properties
        Server
        Node
        SetpointPublisher
        Subscribers = {}
        Timer
        UseROS2 = false
        LastSetpoint = 22.5
        ReceivedSetpoint = false
        CabinTempC = 18.0
        HeaterOn = false
        ACOn = false
        MaintainOn = false
        SensorFault = false
        CommLost = false
        HasCommLost = false
        LastCabinClock
        HasCabinData = false
        LastClientConnected = false
        LastClientClock
        LastError = ''
    end

    methods
        function obj = HVACWindowsBridge(useROS2)
            if nargin < 1
                useROS2 = false;
            end
            obj.UseROS2 = logical(useROS2);
            obj.LastCabinClock = tic;
            obj.LastClientClock = tic;
            if obj.UseROS2
                % Existing Simulink ROS 2 blocks are already configured in domain 0.
                domain = getenv('ROS_DOMAIN_ID');
                if ~isempty(domain) && ~strcmp(domain,'0')
                    error('ROS_DOMAIN_ID=%s. Utiliser le domaine 0 pour notre modele.',domain);
                end
                obj.Node = ros2node('/hvac_windows_tcp_bridge');
                obj.SetpointPublisher = ros2publisher(obj.Node, ...
                    '/hvac/setpoint','std_msgs/Float64');
                obj.Subscribers = {
                    ros2subscriber(obj.Node,'/hvac/cabin_temp', ...
                      'std_msgs/Float64',@(msg)obj.onFloat('cabin',msg))
                    ros2subscriber(obj.Node,'/hvac/heater_cmd', ...
                      'std_msgs/Bool',@(msg)obj.onBool('heater',msg))
                    ros2subscriber(obj.Node,'/hvac/ac_cmd', ...
                      'std_msgs/Bool',@(msg)obj.onBool('ac',msg))
                    ros2subscriber(obj.Node,'/hvac/maintain', ...
                      'std_msgs/Bool',@(msg)obj.onBool('maintain',msg))
                    ros2subscriber(obj.Node,'/hvac/sensor_fault', ...
                      'std_msgs/Bool',@(msg)obj.onBool('fault',msg))
                    ros2subscriber(obj.Node,'/hvac/comm_lost', ...
                      'std_msgs/Bool',@(msg)obj.onBool('comm',msg))
                };
            end

            try
                obj.Server = tcpserver('127.0.0.1',16555);
                configureTerminator(obj.Server,'LF');
                configureCallback(obj.Server,'terminator', ...
                    @(src,~)obj.onTcpLine(src));
                obj.Timer = timer('ExecutionMode','fixedSpacing', ...
                    'Period',0.5,'BusyMode','drop', ...
                    'TimerFcn',@(~,~)obj.onTick());
                start(obj.Timer);
            catch err
                obj.cleanup();
                rethrow(err);
            end
            fprintf('\nBRIDGE TCP : PASS | 127.0.0.1:16555\n');
            if obj.UseROS2
                fprintf('ROS 2 : ACTIVE | domaine 0\n');
            else
                fprintf('ROS 2 : NON ACTIVE (test TCP seul)\n');
            end
            fprintf('Laisser cette session MATLAB ouverte.\n');
        end

        function onTcpLine(obj,src)
            try
                received = strtrim(char(readline(src)));
                data = jsondecode(received);
                if ~isstruct(data) || ~isfield(data,'type')
                    return;
                end
                if strcmp(char(data.type),'setpoint')
                    if ~isfield(data,'value')
                        return;
                    end
                    value = double(data.value);
                    if ~(isnumeric(value) && isscalar(value) && ...
                            isfinite(value) && value >= 16 && value <= 30)
                        obj.sendPacket(struct('type','error', ...
                           'message','Consigne rejetee (16 a 30 C)'));
                        return;
                    end
                    obj.LastSetpoint = value;
                    obj.ReceivedSetpoint = true;
                    obj.LastClientClock = tic;
                    obj.sendPacket(struct('type','ack','setpoint',value));
                    if obj.UseROS2
                        obj.publishSetpoint();
                    end
                end
            catch err
                fprintf('TCP lecture : %s\n',err.message);
            end
        end

        function publishSetpoint(obj)
            try
                msg = ros2message(obj.SetpointPublisher);
                msg.data = double(obj.LastSetpoint);
                send(obj.SetpointPublisher,msg);
            catch err
                obj.LastError = err.message;
                fprintf('ROS publication : %s\n',err.message);
            end
        end

        function onFloat(obj,kind,msg)
            if strcmp(kind,'cabin')
                obj.CabinTempC = double(msg.data);
                obj.LastCabinClock = tic;
                obj.HasCabinData = true;
            end
        end

        function onBool(obj,kind,msg)
            value = logical(msg.data);
            switch kind
                case 'heater'
                    obj.HeaterOn = value;
                case 'ac'
                    obj.ACOn = value;
                case 'maintain'
                    obj.MaintainOn = value;
                case 'fault'
                    obj.SensorFault = value;
                case 'comm'
                    obj.CommLost = value;
                    obj.HasCommLost = true;
            end
        end

        function onTick(obj)
            try
                if isempty(obj.Server) || ~obj.Server.Connected
                    obj.ReceivedSetpoint = false;
                    obj.LastClientConnected = false;
                    return;
                end
                if ~obj.LastClientConnected
                    obj.LastClientConnected = true;
                    fprintf('UNITY CONNECTE au serveur TCP local.\n');
                end
                if obj.UseROS2 && obj.ReceivedSetpoint && ...
                        toc(obj.LastClientClock) < 3.0
                    % 2 Hz periodic publication resets ROS2-15 watchdog.
                    obj.publishSetpoint();
                end
                dataRecent = obj.UseROS2 && obj.HasCabinData && ...
                    toc(obj.LastCabinClock) < 3.0;
                packet = struct( ...
                    'type','telemetry', ...
                    'rosData',logical(dataRecent), ...
                    'cabinTempC',double(obj.CabinTempC), ...
                    'heaterOn',logical(obj.HeaterOn), ...
                    'acOn',logical(obj.ACOn), ...
                    'maintainOn',logical(obj.MaintainOn), ...
                    'sensorFault',logical(obj.SensorFault), ...
                    'commLost',logical(obj.CommLost), ...
                    'hasCommLost',logical(obj.HasCommLost));
                obj.sendPacket(packet);
            catch err
                obj.LastError = err.message;
                fprintf('Bridge tick : %s\n',err.message);
            end
        end

        function sendPacket(obj,packet)
            if ~isempty(obj.Server) && obj.Server.Connected
                writeline(obj.Server,jsonencode(packet));
            end
        end

        function delete(obj)
            obj.cleanup();
        end

        function cleanup(obj)
            if ~isempty(obj.Timer)
                try
                    stop(obj.Timer);
                    delete(obj.Timer);
                catch
                end
                obj.Timer = [];
            end
            if ~isempty(obj.Server)
                try
                    configureCallback(obj.Server,'off');
                catch
                end
                obj.Server = [];
            end
            obj.Subscribers = {};
            obj.SetpointPublisher = [];
            obj.Node = [];
        end
    end
end
