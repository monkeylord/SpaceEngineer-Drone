List<IMyTerminalBlock> list = new List<IMyTerminalBlock>();     
List<IMyTerminalBlock> listT = new List<IMyTerminalBlock>();     
   
//Test Small Drone 0x03   
//Base model   
   
//Settings:   
//Patrol Range (meter)	巡逻半径   
int Range = 30000;   
//Navigation		导航及规避   
Vector3D origin = new Vector3D(0, 0, 0);     
Vector3D target = new Vector3D(0, 0, 0);     
   
//距离设定   
int DstDodge=1000;   
int DstFire=800;   
int DstSafe=800;   
int DstChaseFireRange=1200; 
int MaxAttackDst = 700;   
int MinAttackDst = 600;   
 
   
int Tick=1;   
int debugcount=0;   
UnionControl uc=null; 
void Main(string argument)     
{     
	try{   
		if(!PilotCheck()){Echo("MalFunctional");return;}//基本自检   
		if(uc==null){ 
			uc=new UnionControl(this,remote); 
			uc.Manual(true); 
		}else uc.Update(); 
		if(!TargetAcquire(argument))stgNow=Strategys.Return;//获取目标   
		WeaponCheck();//武器状态检查   
		handleStg(stgNow);//开始执行策略   
		if(LastAquired>300){   
			GridTerminalSystem.GetBlocksOfType<IMyRadioAntenna>(list);   
			if(list.Count>0)list[0].SetCustomName("Target Lost");   
		}   
		if(list.Count>1)list[1].SetCustomName(debugcount.ToString());   
		debugcount++;   
	}catch(Exception e){   
		GridTerminalSystem.GetBlocksOfType<IMyRadioAntenna>(list);   
		if(list.Count>0)list[0].SetCustomName(e.ToString());   
	}   
}   
   
//检查开始   
   
//武器检查   
bool MannualWeapon=true; 
bool AutoWeapon=false; 
List<IMyUserControllableGun> ManualGun=new List<IMyUserControllableGun>(); 
private bool WeaponCheck()   
{   
	//TODO   
	if(ManualGun.Count==0)GridTerminalSystem.GetBlocksOfType<IMyUserControllableGun>(ManualGun);      
	MannualWeapon=true;   
	AutoWeapon=false;   
	return true;   
} 
 
//基本自检   
IMyRemoteControl remote = null;   
IMyTerminalBlock timer = null;   
Vector3D MyPos = new Vector3D(0, 0, 0);     
Vector3D MySpeed = new Vector3D(0, 0, 0);     
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
	Tick=1;   
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
	/* 
	GridTerminalSystem.GetBlocksOfType<IMyRadioAntenna>(list);   
	if(list.Count>0)Vector3D.TryParse(target, out pos);     
	else */	remote.GetNearestPlayer(out pos);   
	   
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
   
//策略部分开始   
   
//策略   
enum Strategys { Stasis, Return, QuickApproach, AimAndShoot, Dodge, Chase, Dock, Suicide };   
Strategys stgNow=Strategys.Stasis;   
Strategys stgLast=Strategys.Stasis;   
bool State_BeingChased=false; 
bool State_Chasing=false; 
bool State_Disengage=false; 
//总控   
void handleStg(Strategys stg){   
	stgLast=stgNow; 
	StateCheck(); 
	if(State_Disengage)stgNow=Strategys.QuickApproach; 
	else if(State_BeingChased)stgNow=Strategys.Dodge; 
	else stgNow=stg;   
	switch(stgNow){   
		case Strategys.Stasis:   
			Strategy_Stasis();   
			break;   
		case Strategys.Return:   
			Strategy_ReturnOrigin();   
			break;   
		case Strategys.QuickApproach:   
			Strategy_QuickApproach();   
			break;   
		case Strategys.AimAndShoot:   
			Strategy_AimAndShoot();   
			break;    
		case Strategys.Dodge:   
			Strategy_Dodge();   
			break; 
		case Strategys.Chase: 
			Strategy_Chase(); 
			break;   
		case Strategys.Dock:   
			break;   
		case Strategys.Suicide:   
			break;   
	} 
	Echo(stgNow.ToString()+"Done"); 
	//remote.SetAutoPilotEnabled(true);       
}   
   
void StateCheck(){   
	//定义各类情况   
	State_BeingChased=((TargetSpeedPerTick*60).Length()>30&&Vector3D.Dot(Vector3D.Normalize(TargetSpeedPerTick),Vector3D.Normalize(MyPos-Target))>0.85&&TargetDistance < DstSafe*2)?true:false; 
	State_Chasing=(Vector3D.Dot(TargetSpeedPerTick*0.6,MySpeed)>80)?true:false; 
	State_Disengage=(TargetDistance > DstSafe * 3 && LastAquired<300)?true:false; 
}   
   
private void Strategy_ReturnOrigin(){   
	uc.Output(100f); 
	uc.Heading(origin);     
	//5 km 安全休眠距离   
	if (Vector3D.Distance(Target, remote.GetPosition ()) > 5000)handleStg(Strategys.Stasis);   
}   
   
private void Strategy_Stasis(){   
	if(Engage){uc.Inertia(false);uc.Output(0f);handleStg(Strategys.QuickApproach);}   
	else uc.Inertia(true);   
}   
   
Vector3D dodge;   
private void Strategy_Chase(){   
	if(TargetDistance>=DstSafe*2){   
		stgNow=Strategys.QuickApproach;  
	}else{   
		Vector3D TargetFixed=HitPointCaculateV3(MyPos, MySpeed, new Vector3D(0), Target, TargetSpeedPerTick*60, TargetAcceleration*60, 400, 1, 1.55, false); 
		uc.Heading(TargetFixed);  
		Random ran=new Random(); 
		if(debugcount%60==0)sign=(ran.Next(-50,50)<0)?sign*-1:sign; 
		uc.Output(Vector3D.Normalize(Vector3D.Cross(MySpeed,MyPos-Target)*sign)*85+Vector3D.Normalize(RandomMove())*15); 
	}  
}   
private void Strategy_QuickApproach(){   
	if(TargetDistance<DstDodge){   
		handleStg(Strategys.AimAndShoot);   
	}else{   
		uc.Output(100f);   
		uc.Heading(Target+TargetSpeedPerTick*60);   
	}   
}   
 
int sign=1; 
private void Strategy_AimAndShoot(){   
	Echo("Attacking");   
	if(remote.RotationIndicator.Length()<0.05 && TargetDistance<DstFire+Vector3D.Dot(TargetSpeedPerTick,Vector3D.Normalize(MySpeed))*120)ManualShot();   
	Vector3D TargetFixed=HitPointCaculateV3(MyPos, MySpeed, new Vector3D(0), Target, TargetSpeedPerTick*60, TargetAcceleration*60, 400, 1, 1.55, false); 
	uc.Heading(TargetFixed);    
	//Vector3D DstKeepV=(TargetDistance<MinAttackDst||TargetDistance>MaxAttackDst)?Vector3D.Normalize(MyPos-Target)*(TargetDistance-(MinAttackDst+MaxAttackDst)/2):new Vector3D(0,0,0); 
	Vector3D DstKeepV=Vector3D.Normalize(MyPos-Target)*Math.Min(Math.Max((TargetDistance-(MinAttackDst+MaxAttackDst)/2),-200),200);
	Random ran=new Random(); 
	if(debugcount%60==0)sign=(ran.Next(-50,50)<0)?sign*-1:sign; 
	uc.Output(((State_Chasing)?Vector3D.Zero:DstKeepV)+Vector3D.Normalize(Vector3D.Cross(MySpeed,MyPos-Target)*sign)*50+Vector3D.Normalize(RandomMove())*15); 
}  
   
   
//紧急闪避   
Vector3D edodge=new Vector3D(0,0,0);   
private void Strategy_Dodge(){   
	if(TargetSpeedPerTick.Length()<1||TargetDistance>DstSafe*2){   
		stgNow=Strategys.AimAndShoot;   
	} 
	Vector3D MyTargetAcc=(TargetAcceleration.Length()<1)?new Vector3D(1,0,0):TargetAcceleration; 
	edodge = Vector3D.Normalize(Vector3D.Cross(TargetSpeedPerTick,MyTargetAcc))*80; 
	uc.Output(MyPos+edodge+RandomMove()*20); 
	uc.Heading(HitPointCaculateV3(MyPos, MySpeed, new Vector3D(0), Target, TargetSpeedPerTick*60, TargetAcceleration*60, 400, 1, 1.55, false)); 
	if(TargetDistance<DstChaseFireRange)ManualShot(); 
}   
//策略部分结束   
   
   
//功能部分开始   
 
//旧算法v3.0 
Vector3D HitPointCaculateV3(Vector3D Me_Position, Vector3D Me_Velocity, Vector3D Me_Acceleration, Vector3D Target_Position, Vector3D Target_Velocity, Vector3D Target_Acceleration, double Bullet_Speed, double Velocity_Factor, double Acceleration_Factor, bool AbsolutelyBullet) 
{ 
	double HitTime = Vector3D.Distance(Me_Position, Target_Position)/Bullet_Speed; 
	 
	Vector3D Me_Velocity_Effect = new Vector3D(); 
	if(AbsolutelyBullet) 
	{ 
		Me_Velocity_Effect = new Vector3D(0,0,0); 
	} 
	else 
	{ 
		Me_Velocity_Effect = Me_Velocity; 
	} 
	 
	Vector3D HitPosition = Target_Position + (Target_Velocity+(0.5*Target_Acceleration*HitTime))*HitTime - Me_Velocity_Effect*HitTime; 
	 
	HitTime = Vector3D.Distance(HitPosition, Me_Position)/Bullet_Speed; 
	HitPosition = Target_Position + (Target_Velocity*Velocity_Factor+(0.5*Target_Acceleration*HitTime*Acceleration_Factor))*HitTime - Me_Velocity_Effect*HitTime*Velocity_Factor; 
	 
	//多次逼近修正 
	HitTime = Vector3D.Distance(HitPosition, Me_Position)/Bullet_Speed; 
	HitPosition = Target_Position + (Target_Velocity*Velocity_Factor+(0.5*Target_Acceleration*HitTime*Acceleration_Factor))*HitTime - Me_Velocity_Effect*HitTime*Velocity_Factor; 
	HitTime = Vector3D.Distance(HitPosition, Me_Position)/Bullet_Speed; 
	HitPosition = Target_Position + (Target_Velocity*Velocity_Factor+(0.5*Target_Acceleration*HitTime*Acceleration_Factor))*HitTime - Me_Velocity_Effect*HitTime*Velocity_Factor; 
	HitTime = Vector3D.Distance(HitPosition, Me_Position)/Bullet_Speed; 
	HitPosition = Target_Position + (Target_Velocity*Velocity_Factor+(0.5*Target_Acceleration*HitTime*Acceleration_Factor))*HitTime - Me_Velocity_Effect*HitTime*Velocity_Factor; 
	HitTime = Vector3D.Distance(HitPosition, Me_Position)/Bullet_Speed; 
	HitPosition = Target_Position + (Target_Velocity*Velocity_Factor+(0.5*Target_Acceleration*HitTime*Acceleration_Factor))*HitTime - Me_Velocity_Effect*HitTime*Velocity_Factor; 
	HitTime = Vector3D.Distance(HitPosition, Me_Position)/Bullet_Speed; 
	HitPosition = Target_Position + (Target_Velocity*Velocity_Factor+(0.5*Target_Acceleration*HitTime*Acceleration_Factor))*HitTime - Me_Velocity_Effect*HitTime*Velocity_Factor; 
	 
	return HitPosition; 
}
//闪避算法   
Vector3D r60=new Vector3D(0,0,0); 
Vector3D r30=new Vector3D(0,0,0); 
Vector3D r10=new Vector3D(0,0,0); 
private Vector3D RandomMove(){ 
	Random ran=new Random();  
	if(debugcount%61==0)r60=new Vector3D(ran.Next(-450,450),ran.Next(-450,450), ran.Next(-450,450)); 
	if(debugcount%31==0)r30=new Vector3D(ran.Next(-450,450),ran.Next(-450,450), ran.Next(-450,450)); 
	if(debugcount%11==0)r10=new Vector3D(ran.Next(-450,450),ran.Next(-450,450), ran.Next(-450,450)); 
	return r60*0.1+r30+0.2+r10*0.3+new Vector3D(ran.Next(-450,450),ran.Next(-450,450), ran.Next(-450,450))*0.4; 
}  
   
//武器控制 
private bool ManualShot()   
{ 
	IMyUserControllableGun GG; 
	foreach(IMyUserControllableGun manualGun in ManualGun)manualGun.GetActionWithName("ShootOnce").Apply(manualGun);   
	return true;   
} 
public class UnionControl{
	
	MyGridProgram MGP;
	IMyShipController SettedMain;	//指定主控
	IMyShipController Main;			//当前主控
	Vector3D MyPos;
	Vector3D MyVelocity;
	Vector3D CruiseVelocity=new Vector3D(0);
	Vector3D CruiseDirection=new Vector3D(0);
	Vector3D faceToDir=new Vector3D(0);
	bool IsCruising=false;
	bool InertiaDump=true;
	String LastMsg;
	List<IMyGyro> Gyroscopes = new List<IMyGyro>();
	List<IMyGravityGenerator> GGs =  new List<IMyGravityGenerator>();
	List<IMyThrust> Thrusts = new List<IMyThrust>();
	Dictionary<IMyTerminalBlock,Base6Directions.Direction> Directions=new Dictionary<IMyTerminalBlock,Base6Directions.Direction>();
	//陀螺仪控制
	string[] gyroYawField = null;
	string[] gyroPitchField = null;
	string[] gyroRollField = null;
	float[] gyroYawFactor = null;
	float[] gyroPitchFactor = null;
	float[] gyroRollFactor = null;
	
	bool ManualControl=false;
	Vector3D OutputVector=new Vector3D(0);
	Vector3D CalcumRotation=new Vector3D(0);
	//public void Update();
	//public bool Cruise(bool Cruise);
	//public bool Inertia(bool inertia);
	//public bool Manual(bool OnOff);
	//public void Heading(Vector3D headingTo);
	//public void VectorMove(Vector3D Power,Vector3D headingTo);
	//public void Output(Vector3D outputVector);
	//public void Output(float output);	
	public UnionControl(MyGridProgram mgp,IMyShipController SC=null){
		MGP=mgp;
		SettedMain=SC;
		if(SettedMain!=null)Main=SC;
		Init();
		Update();
	}
	public void Update(){
		//如果主控未设置则寻找主控，如果主控有变化，则重新初始化
		if(SettedMain==null){
			List<IMyShipController> list = new List<IMyShipController>(); 
			MGP.GridTerminalSystem.GetBlocksOfType<IMyShipController> (list);
			IMyShipController MainFound=null;
			foreach(IMyShipController sc in list)if(sc.IsUnderControl)MainFound=sc;
			if(Main!=MainFound && MainFound!=null){Main=MainFound;Init();}
			else if(Main==null)return;
		}else Main=SettedMain;
		//TODO	更新当前速度、加速度、旋转状态，更新当前操作状态
		MyPos=Main.GetPosition();
		MyVelocity=Main.GetShipVelocities().LinearVelocity;
		InertiaDump=(ManualControl)?InertiaDump:Main.DampenersOverride;
		Vector3 MoveIndicator=Main.MoveIndicator;
		MatrixD refLookAtMatrix = MatrixD.CreateLookAt(new Vector3D(0,0,0), Main.WorldMatrix.Forward, Main.WorldMatrix.Up);
		Vector3D rotation = (ManualControl)?Gyro(faceToDir):new Vector3D(Main.RotationIndicator.Y,-Main.RotationIndicator.X,-Main.RollIndicator*100);		//陀螺仪方向
		//控制陀螺仪惯性
		Vector3D AngularVectorToMe = Vector3D.TransformNormal(Main.GetShipVelocities().AngularVelocity, refLookAtMatrix); 
		CalcumRotation=(CalcumRotation*4+new Vector3D(AngularVectorToMe.Y,-AngularVectorToMe.X,-AngularVectorToMe.Z))/5;
		rotation = rotation - new Vector3D(AngularVectorToMe.Y,-AngularVectorToMe.X,-AngularVectorToMe.Z)*0.6 - CalcumRotation*0.4;
		SetGyroYPR(rotation);
		//将飞船速度转换坐标系为相对速度

		Vector3D relativeVelocity = new Vector3D(0);
		if(MyVelocity.Length()>0)relativeVelocity = Vector3D.TransformNormal(MyVelocity, refLookAtMatrix);	//坐标系转换后的相对速度
		Vector3D relativeCruiseVelocity = new Vector3D(0);
		if(CruiseVelocity.Length()>0)relativeCruiseVelocity = Vector3D.TransformNormal(CruiseVelocity, refLookAtMatrix);
		//判断惯性控制输出
		Vector3D output=(ManualControl)?OutputVector:new Vector3D(MoveIndicator.X*-100f,MoveIndicator.Y*-100f,MoveIndicator.Z*-100f);// 从控制到出力转换
		Vector3D expectVelocity=(output.Length()>0)?Vector3D.Normalize(output)*-100:Vector3D.Zero;
		if(InertiaDump){
			output.X=CalcInertiaDump(relativeVelocity.X-expectVelocity.X);
			output.Y=CalcInertiaDump(relativeVelocity.Y-expectVelocity.Y);
			output.Z=CalcInertiaDump(relativeVelocity.Z-expectVelocity.Z);
			//output=(output.Length()>0)?Vector3D.Normalize(output)*100:Vector3D.Zero;
		}else{
			output=-expectVelocity;
		}
		MGP.Echo(output.ToString());
		GEngine(output);
		TEngine(output);
		SEngine(output);
	}
	
	public bool Cruise(bool Cruise){
		IsCruising=Cruise;
		if(IsCruising){
			CruiseVelocity=MyVelocity;
		}else{
			CruiseVelocity=new Vector3D(0);
		}
		LastMsg="Crusing is Setted on "+IsCruising;
		return IsCruising;
	}
	public bool Inertia(bool inertia){
		InertiaDump=inertia;
		LastMsg="InertiaDump is Setted on "+InertiaDump;
		return InertiaDump;
	}
	public bool Manual(bool OnOff){
		ManualControl=OnOff;
		return ManualControl;
	}
	public void Heading(Vector3D headingTo){
		faceToDir=headingTo;
	}
	public void Output(Vector3D outputVector){
		MatrixD refLookAtMatrix = MatrixD.CreateLookAt(new Vector3D(0,0,0), Main.WorldMatrix.Forward, Main.WorldMatrix.Up);
		OutputVector=Vector3D.Normalize(Vector3D.TransformNormal(outputVector, refLookAtMatrix))*outputVector.Length();
	}
	public void OutputRelative(Vector3D outputVector){
		OutputVector=outputVector;
	}
	public void Output(float output){
		OutputVector=new Vector3D(0,0,output);
	}
	public void VectorMove(Vector3D Power,Vector3D headingTo){
		//TODO	根据推进器最大出力，将方向矢量转换为出力矢量。
	}
	private void GEngine(Vector3D output){
		//TODO	六向重力控制
		foreach(IMyGravityGenerator GG in GGs){
			switch(Directions[GG]){
				case Base6Directions.Direction.Forward:
					GG.SetValue("Gravity",Convert.ToSingle(output.Z));
				break;
				case Base6Directions.Direction.Backward:
					GG.SetValue("Gravity",Convert.ToSingle(-output.Z));
				break;
				case Base6Directions.Direction.Left:
					GG.SetValue("Gravity",Convert.ToSingle(output.X));
				break;
				case Base6Directions.Direction.Right:
					GG.SetValue("Gravity",Convert.ToSingle(-output.X));
				break;
				case Base6Directions.Direction.Down:
					GG.SetValue("Gravity",Convert.ToSingle(output.Y));
				break;
				case Base6Directions.Direction.Up:
					GG.SetValue("Gravity",Convert.ToSingle(-output.Y));
				break;
			}
		}
	}
	private void TEngine(Vector3D output){
		//TODO	六向常规推力
		foreach(IMyThrust Thrust in Thrusts){
			switch(Directions[Thrust]){
				case Base6Directions.Direction.Forward:
					Thrust.ApplyAction((output.Z>0)?"OnOff_On":"OnOff_Off");
					Thrust.SetValue("Override",Convert.ToSingle((output.Z>0)?output.Z:0f));
				break;
				case Base6Directions.Direction.Backward:
					Thrust.ApplyAction((output.Z<0)?"OnOff_On":"OnOff_Off");
					Thrust.SetValue("Override",Convert.ToSingle((output.Z<0)?-output.Z:0f));
				break;
				case Base6Directions.Direction.Left:
					Thrust.ApplyAction((output.X>0)?"OnOff_On":"OnOff_Off");
					Thrust.SetValue("Override",Convert.ToSingle((output.X>0)?output.X:0f));
				break;
				case Base6Directions.Direction.Right:
					Thrust.ApplyAction((output.X<0)?"OnOff_On":"OnOff_Off");
					Thrust.SetValue("Override",Convert.ToSingle((output.X<0)?-output.X:0f));
				break;
				case Base6Directions.Direction.Down:
					Thrust.ApplyAction((output.Y>0)?"OnOff_On":"OnOff_Off");
					Thrust.SetValue("Override",Convert.ToSingle((output.Y>0)?output.Y:0f));
				break;
				case Base6Directions.Direction.Up:
					Thrust.ApplyAction((output.Y<0)?"OnOff_On":"OnOff_Off");
					Thrust.SetValue("Override",Convert.ToSingle((output.Y<0)?-output.Y:0f));
				break;
			}
		}
	}
	private void SEngine(Vector3D output){
		//TODO	非常规推力
	}
	private Vector3D Gyro(Vector3D faceToDir){
		//TODO	航向保持
		MatrixD refLookAtMatrix = MatrixD.CreateLookAt(new Vector3D(0,0,0), Main.WorldMatrix.Forward, Main.WorldMatrix.Up);
		Vector3D indicator=Vector3D.Normalize(Vector3D.TransformNormal(faceToDir-Main.GetPosition(), refLookAtMatrix))*40;
		indicator.Z=0f;
		return indicator;
	}
	private void Init(){
		//六向陀螺仪统计
		Gyroscopes.Clear();
		MGP.GridTerminalSystem.GetBlocksOfType<IMyGyro> (Gyroscopes);
		GyroInit();
		
		MatrixD refWorldMatrix = Main.WorldMatrix;
		Directions.Clear();
		//TODO	六向重力引擎统计
		GGs.Clear();
		MGP.GridTerminalSystem.GetBlocksOfType<IMyGravityGenerator> (GGs);
		foreach(IMyGravityGenerator GG in GGs){
			Directions.Add(GG,refWorldMatrix.GetClosestDirection(GG.WorldMatrix.Down));
		}
		//TODO	六向推进器统计
		Thrusts.Clear();
		MGP.GridTerminalSystem.GetBlocksOfType<IMyThrust> (Thrusts);
		foreach(IMyThrust Thrust in Thrusts){
			Directions.Add(Thrust,refWorldMatrix.GetClosestDirection(Thrust.WorldMatrix.Backward));
		}
		//TODO 统一处理方向
		

	}
	private float CalcInertiaDump(double Speed){
		if(Speed>0.1)return 100f;
		else if(Speed<-0.1)return -100f;
		else if(Math.Abs(Speed)<0.001)return 0f;
		else return (float)Speed*60;
	}
	
	private void SetGyroYPR(Vector3D Rate)
	{
		for (int i = 0; i < Gyroscopes.Count; i++)
		{
			Gyroscopes[i].SetValue(gyroYawField[i], (float)Rate.X * gyroYawFactor[i]);
			Gyroscopes[i].SetValue(gyroPitchField[i], (float)Rate.Y * gyroPitchFactor[i]);
			Gyroscopes[i].SetValue(gyroRollField[i], (float)Rate.Z * gyroRollFactor[i]);
		}
	}
	const float GYRO_FACTOR = (float)(Math.PI / 30);
	private void GyroInit()
	{
		//处理陀螺仪
		MatrixD refWorldMatrix=Main.WorldMatrix;
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
				if(!Gyroscopes[i].GyroOverride)Gyroscopes[i].ApplyAction("Override");
			}
		}
	}
}