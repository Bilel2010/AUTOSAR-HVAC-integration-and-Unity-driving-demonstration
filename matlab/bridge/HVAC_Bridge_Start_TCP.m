%% ETAPE 1 : test TCP local (sans Simulink, sans ROS 2)
clc;
cd('D:\AUTOSAR_HVAC_Project\WindowsBridge\MATLAB');
assert(~isempty(which('tcpserver')), ...
    'tcpserver indisponible dans cette installation MATLAB.');
if exist('hvacBridge','var') && isa(hvacBridge,'HVACWindowsBridge')
    delete(hvacBridge);
    clear hvacBridge
end
hvacBridge = HVACWindowsBridge(false);
assignin('base','hvacBridge',hvacBridge);
disp('ETAPE 1 : dans Unity, activer HVAC/03 puis Play.');
