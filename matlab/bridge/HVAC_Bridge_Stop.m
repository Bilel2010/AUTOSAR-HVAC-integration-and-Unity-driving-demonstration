%% Arret propre du serveur et de ses abonnements ROS 2
if evalin('base','exist(''hvacBridge'',''var'')')
    evalin('base','delete(hvacBridge); clear hvacBridge');
end
disp('Bridge Windows : ARRETE');
