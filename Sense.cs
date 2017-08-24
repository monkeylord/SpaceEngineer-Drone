//基本自检
IMyRemoteControl remote = null;
IMyTerminalBlock timer = null;
Vector3D MyPos = new Vector3D(0, 0, 0);  
Vector3D MySpeed = new Vector3D(0, 0, 0);  
Vector3D MyAcceleration = new Vector3D(0,0,0);
private bool PilotCheck()
{
	if(remote==null||remote.IsFunctional==false){
		remote=null;
		GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(list);  
		for(int i=0;i<list.Count;i++)  
		{  
			if(list[i].IsFunctional){
				remote = list[i] as IMyRemoteControl; 
				break;
			}
		}
		if(remote==null){
			Echo("RemoteControl Not Found");
			return false;
		};
	}
	MyAcceleration=MySpeed-remote.GetShipVelocities().LinearVelocity;
	MySpeed=remote.GetShipVelocities().LinearVelocity;
	MyPos=remote.GetPosition();
	
	if(timer==null||timer.IsFunctional==false){
		timer=null;
		GridTerminalSystem.GetBlocksOfType<IMyTimerBlock>(list);  
		for(int i=0;i<list.Count;i++)  
		{  
			if(list[i].IsFunctional){
				timer = list[i] as IMyTimerBlock; 
				break;
			}
		}
		if(timer==null){
			Echo("Timer Not Found");
			return false;
		};
	}
	if(Ticking)Tick=1;
	else Tick=60;
	Ticking=false;
	return true;
}

//目标获取
bool Engage=false;
Vector3D Target = new Vector3D(0, 0, 0);  
Vector3D TargetSpeedPerTick = new Vector3D(0, 0, 0);
Vector3D TargetAcceleration = new Vector3D(0, 0, 0);
double TargetDistance=0;
float TargetAngle=0;
int LastAquired=0;
private bool TargetAcquire(string target){
	Vector3D pos;
	Engage=false;
	GridTerminalSystem.GetBlocksOfType<IMyRadioAntenna>(list);
	if(list.Count>0)Vector3D.TryParse(target, out pos);  
	else remote.GetNearestPlayer(out pos);
	
	if(pos!=new Vector3D(0,0,0)&&pos!=null){
		LastAquired=0;
		TargetAcceleration=((pos-Target)/Tick-TargetSpeedPerTick)/Tick*60;
		TargetSpeedPerTick=(pos-Target)/Tick;
		Target=pos;
		TargetDistance=Vector3D.Distance(Target, MyPos);
	}
	else LastAquired+=Tick;
	
	if(LastAquired>600)return false;
	TargetDistance=Vector3D.Distance(Target, MyPos);
	//TODO
	TargetAngle=0;
	
	
	Echo(TargetDistance.ToString());
	Engage=true;
	return Engage;
}

//态势感知结束