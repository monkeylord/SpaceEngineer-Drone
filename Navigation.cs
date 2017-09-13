class Navi{
	public Navi(IMyTerminalBlock baseblock){
		
	}
	public bool FaceTo(Vector3D direction){
		
	}
	public bool Engine(float speed){
		
	}
	public bool Engine(Vector3D speedVec){
		
	}
	public bool Stasis(bool onoff){
		
	}
	

//瞄准方向控制
enum FaceModes {Remote,Gyro};
FaceModes FaceMode=FaceModes.Gyro;
int TimePast=0;
void FaceTo(Vector3D pos,string description)
{
	switch(FaceMode){
		case FaceModes.Gyro:
		Vector3D targetposition = pos;
		MatrixD refLookAtMatrix = MatrixD.CreateLookAt(new Vector3D(0,0,0), remote.WorldMatrix.Forward, remote.WorldMatrix.Up);
		Vector3D targetVector = Vector3D.Normalize(Vector3D.TransformNormal(targetposition - remote.GetPosition(), refLookAtMatrix));//得出目标方位单位矢量
		//这里获得目标相对Remote的归一化向量，X是左右，左-右+，Y是上下，上-下+
		//这里应为是星球飞行器，只控制左右转向即可，陀螺仪的Yaw是控制左右，左-右+
		Echo(targetVector.ToString());
		SetGyroOverride(true);
		SetGyroYaw(40*targetVector.X);
		SetGyroPitch(40*targetVector.Y);
		//SetGyroRoll(40*targetVector.Z);
		nextTick();
		break;
		case FaceModes.Remote:
		if(TimePast%15==0){
			remote.ClearWaypoints();  
			remote.AddWaypoint(pos, description);
			remote.SetAutoPilotEnabled(true); 
		}
		break;
	}
	StringBuilder sb=new StringBuilder();
	sb.Append(description);
	sb.Append(" ");
	sb.Append(TimePast.ToString());
	TimePast++;
	GridTerminalSystem.GetBlocksOfType<IMyRadioAntenna>(list);
	if(list.Count>0)list[0].SetCustomName(sb);
}	
//以下是陀螺仪控制
void SetGyroOverride(bool bOverride)
{
	for (int i = 0; i < Gyroscopes.Count; i++)
	{
		if (((IMyGyro)Gyroscopes[i]).GyroOverride != bOverride)
		{
			Gyroscopes[i].ApplyAction("Override");
		}
	}
}

void SetGyroYaw(double yawRate)
{
	for (int i = 0; i < Gyroscopes.Count; i++)
	{
		Gyroscopes[i].SetValue(gyroYawField[i], (float)yawRate * gyroYawFactor[i]);
	}
}

void SetGyroPitch(double pitchRate)
{
	for (int i = 0; i < Gyroscopes.Count; i++)
	{
		Gyroscopes[i].SetValue(gyroPitchField[i], (float)pitchRate * gyroPitchFactor[i]);
	}
}

void SetGyroRoll(double rollRate)
{
	for (int i = 0; i < Gyroscopes.Count; i++)
	{
		Gyroscopes[i].SetValue(gyroRollField[i], (float)rollRate * gyroRollFactor[i]);
	}
}


//方块
List<IMyTerminalBlock> Gyroscopes;

//导航控制相关
string[] gyroYawField = null;
string[] gyroPitchField = null;
string[] gyroRollField = null;
float[] gyroYawFactor = null;
float[] gyroPitchFactor = null;
float[] gyroRollFactor = null;

//方位计算相关
MatrixD refWorldMatrix; //船只矩阵
const float GYRO_FACTOR = (float)(Math.PI / 30);
Vector3D Y_VECTOR = new Vector3D(0, -1, 0);
Vector3D Z_VECTOR = new Vector3D(0, 0, -1);
Vector3D POINT_ZERO = new Vector3D(0, 0, 0);

bool GyroInited = false;
void GyroInit(IMyTerminalBlock baseblock)
{
	//处理陀螺仪
	refWorldMatrix=baseblock.WorldMatrix;
	Gyroscopes = new List<IMyTerminalBlock>();
	GridTerminalSystem.GetBlocksOfType<IMyGyro> (Gyroscopes);
	if(Gyroscopes.Count > 0)
	{
		gyroYawField = new string[Gyroscopes.Count];
		gyroPitchField = new string[Gyroscopes.Count];
		gyroYawFactor = new float[Gyroscopes.Count];
		gyroPitchFactor = new float[Gyroscopes.Count];
		gyroRollField = new string[Gyroscopes.Count];
		gyroRollFactor = new float[Gyroscopes.Count];
		for (int i = 0; i < Gyroscopes.Count; i++)
		{
			Base6Directions.Direction gyroUp = Gyroscopes[i].WorldMatrix.GetClosestDirection(refWorldMatrix.Up);
			Base6Directions.Direction gyroLeft = Gyroscopes[i].WorldMatrix.GetClosestDirection(refWorldMatrix.Left);
			Base6Directions.Direction gyroForward = Gyroscopes[i].WorldMatrix.GetClosestDirection(refWorldMatrix.Forward);

			switch (gyroUp)
			{
			case Base6Directions.Direction.Up:
				gyroYawField[i] = "Yaw";
				gyroYawFactor[i] = GYRO_FACTOR;
				break;
			case Base6Directions.Direction.Down:
				gyroYawField[i] = "Yaw";
				gyroYawFactor[i] = -GYRO_FACTOR;
				break;
			case Base6Directions.Direction.Left:
				gyroYawField[i] = "Pitch";
				gyroYawFactor[i] = GYRO_FACTOR;
				break;
			case Base6Directions.Direction.Right:
				gyroYawField[i] = "Pitch";
				gyroYawFactor[i] = -GYRO_FACTOR;
				break;
			case Base6Directions.Direction.Forward:
				gyroYawField[i] = "Roll";
				gyroYawFactor[i] = -GYRO_FACTOR;
				break;
			case Base6Directions.Direction.Backward:
				gyroYawField[i] = "Roll";
				gyroYawFactor[i] = GYRO_FACTOR;
				break;
			}

			switch (gyroLeft)
			{
			case Base6Directions.Direction.Up:
				gyroPitchField[i] = "Yaw";
				gyroPitchFactor[i] = GYRO_FACTOR;
				break;
			case Base6Directions.Direction.Down:
				gyroPitchField[i] = "Yaw";
				gyroPitchFactor[i] = -GYRO_FACTOR;
				break;
			case Base6Directions.Direction.Left:
				gyroPitchField[i] = "Pitch";
				gyroPitchFactor[i] = GYRO_FACTOR;
				break;
			case Base6Directions.Direction.Right:
				gyroPitchField[i] = "Pitch";
				gyroPitchFactor[i] = -GYRO_FACTOR;
				break;
			case Base6Directions.Direction.Forward:
				gyroPitchField[i] = "Roll";
				gyroPitchFactor[i] = -GYRO_FACTOR;
				break;
			case Base6Directions.Direction.Backward:
				gyroPitchField[i] = "Roll";
				gyroPitchFactor[i] = GYRO_FACTOR;
				break;
			}

			switch (gyroForward)
			{
			case Base6Directions.Direction.Up:
				gyroRollField[i] = "Yaw";
				gyroRollFactor[i] = GYRO_FACTOR;
				break;
			case Base6Directions.Direction.Down:
				gyroRollField[i] = "Yaw";
				gyroRollFactor[i] = -GYRO_FACTOR;
				break;
			case Base6Directions.Direction.Left:
				gyroRollField[i] = "Pitch";
				gyroRollFactor[i] = GYRO_FACTOR;
				break;
			case Base6Directions.Direction.Right:
				gyroRollField[i] = "Pitch";
				gyroRollFactor[i] = -GYRO_FACTOR;
				break;
			case Base6Directions.Direction.Forward:
				gyroRollField[i] = "Roll";
				gyroRollFactor[i] = -GYRO_FACTOR;
				break;
			case Base6Directions.Direction.Backward:
				gyroRollField[i] = "Roll";
				gyroRollFactor[i] = GYRO_FACTOR;
				break;
			}
			Gyroscopes[i].ApplyAction("OnOff_On");
			SetGyroOverride(false);
		}
	}
	
	GyroInited = true;
}
}


//计时器频率控制
//其中陀螺仪手动控制，来自于MEA群主的算法
private void nextTick()
{
	timer.GetActionWithName("TriggerNow").Apply(timer);
	Ticking=true;
}
private void Stasis(bool status)
{
	if(status)
	{
		//进入休眠频率
		if(timer != null)timer.SetValue("TriggerDelay",SleepFrq);
		//越级控制结束
		GridTerminalSystem.GetBlocksOfType<IMyGravityGenerator>(listT);
		for(int i=0;i<listT.Count;i++)
		{
			((IMyGravityGenerator)listT[i]).GetActionWithName("OnOff_Off").Apply(listT[i]);
		}
		GridTerminalSystem.GetBlocksOfType<IMyVirtualMass>(listT);
		for(int i=0;i<listT.Count;i++)
		{
			((IMyVirtualMass)listT[i]).GetActionWithName("OnOff_Off").Apply(listT[i]);
		}		
		Gear(0.0f);
		
	}
	else
	{
		//进入战斗频率
		if(timer != null)timer.SetValue("TriggerDelay",WarFrq);
		GridTerminalSystem.GetBlocksOfType<IMyGravityGenerator>(listT);
		for(int i=0;i<listT.Count;i++)
		{
			((IMyGravityGenerator)listT[i]).GetActionWithName("OnOff_On").Apply(listT[i]);
		}
		GridTerminalSystem.GetBlocksOfType<IMyVirtualMass>(listT);
		for(int i=0;i<listT.Count;i++)
		{
			((IMyVirtualMass)listT[i]).GetActionWithName("OnOff_On").Apply(listT[i]);
		}
	}
	
	
}
void GStasis(bool status){
	if(status)
	{
		GridTerminalSystem.GetBlocksOfType<IMyGravityGenerator>(listT);
		for(int i=0;i<listT.Count;i++)
		{
			((IMyGravityGenerator)listT[i]).GetActionWithName("OnOff_Off").Apply(listT[i]);
		}
		GridTerminalSystem.GetBlocksOfType<IMyVirtualMass>(listT);
		for(int i=0;i<listT.Count;i++)
		{
			((IMyVirtualMass)listT[i]).GetActionWithName("OnOff_Off").Apply(listT[i]);
		}		
		Gear(0.0f);
		
	}
	else
	{
		GridTerminalSystem.GetBlocksOfType<IMyGravityGenerator>(listT);
		for(int i=0;i<listT.Count;i++)
		{
			((IMyGravityGenerator)listT[i]).GetActionWithName("OnOff_On").Apply(listT[i]);
		}
		GridTerminalSystem.GetBlocksOfType<IMyVirtualMass>(listT);
		for(int i=0;i<listT.Count;i++)
		{
			((IMyVirtualMass)listT[i]).GetActionWithName("OnOff_On").Apply(listT[i]);
		}
	}
}




//以下为动力控制

bool GReversed=false;
private void ReverseG(bool Reverse)
{
	GStasis(false);
	if(Reverse^GReversed){
		GridTerminalSystem.GetBlocksOfType<IMyGravityGenerator>(list);   
		for(int i=0;i<list.Count;i++)   
		{ 
			IMyGravityGenerator GG = list[i] as IMyGravityGenerator; 
			GG.SetValue("Gravity", -1*GG.GetValueFloat("Gravity"));   
		}
		GReversed=!GReversed;
	}
}

float GearLevel=0;
float Gear(float level){
		if(!GearInited)GearInit(remote);
		for(int i=0;i<ForwardT.Count;i++)   
		{ 
			IMyThrust Thrust = ForwardT[i] as IMyThrust; 
			if(level>0)Thrust.SetValue("Override",level);
			else Thrust.SetValue("Override",1.0f);
		}

		for(int i=0;i<BackwardT.Count;i++)   
		{ 
			IMyThrust Thrust = BackwardT[i] as IMyThrust; 
			if(level>0)Thrust.SetValue("Override",1.0f);
			else Thrust.SetValue("Override",-level);
		}
		if(Math.Abs(level)>100.0f)ReverseG((level<0)?true:false);
		else GStasis(true);
		GearLevel=level;
		return GearLevel;

}

List<IMyTerminalBlock> BackwardT=new List<IMyTerminalBlock>();
List<IMyTerminalBlock> ForwardT=new List<IMyTerminalBlock>();
bool GearInited=false;
void GearInit(IMyRemoteControl MyRemote){
	GridTerminalSystem.SearchBlocksOfName("FrontThrust",BackwardT);
	GridTerminalSystem.SearchBlocksOfName("BackThrust",ForwardT);
	GearInited=true;
}

