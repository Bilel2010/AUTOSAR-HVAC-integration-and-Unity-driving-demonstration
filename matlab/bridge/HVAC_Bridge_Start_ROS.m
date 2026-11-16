%% ETAPE 2 : pont TCP <-> topics ROS 2 existants dans Simulink
clc;
cd('D:\AUTOSAR_HVAC_Project\WindowsBridge\MATLAB');
assert(exist('ros2node','file')==2,'ROS Toolbox est requis.');
if exist('hvacBridge','var') && isa(hvacBridge,'HVACWindowsBridge')
    delete(hvacBridge);
    clear hvacBridge
end
setenv('ROS_DOMAIN_ID','0');
hvacBridge = HVACWindowsBridge(true);
assignin('base','hvacBridge',hvacBridge);
disp('ETAPE 2 : Unity Play, puis simulation HVAC_ROS2_LinkWatchdog_v1.');
