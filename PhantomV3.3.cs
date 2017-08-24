List<IMyTerminalBlock> list = new List<IMyTerminalBlock>();  
List<IMyTerminalBlock> listT = new List<IMyTerminalBlock>();  

//Phantom V3
//Base model

//Settings:
//Patrol Range (meter)	Ѳ�߰뾶
int Range = 30000;
//Sleeping Exec Frq	����ʱ����Ƶ��
Single SleepFrq = 30;
//War Exec Frq		����ʱ����Ƶ��
Single WarFrq = 1;

//Gravity Shift		����Ư�Ʋ���
int GShift_L = -450;
int GShift_H = 450;
int GShift_Rate = 80;
//Navigation		���������
Vector3D origin = new Vector3D(0, 0, 0);  
Vector3D target = new Vector3D(0, 0, 0);  

//���Ծ����趨
int DstDodge=1200;
int DstFire=1000;
int DstSafe=1000;

//���ƾ����趨
int MaxAttackDst = 700;
int MinAttackDst = 550;
int ProjectileSpeed=400;


bool Ticking=false;
int Tick=1;
int debugcount=0;
void Main(string argument)  
{  
	try{
		if(!PilotCheck()){Echo("MalFunctional");return;}//�����Լ�
		if(!GyroInited)GyroInit(remote);//��ʼ�������ǿ���
		if(!GearInited)GearInit(remote);//��ʼ����������
		if(!TargetAcquire(argument))stgNow=Strategys.Return;//��ȡĿ��
		WeaponCheck();//����״̬���
		handleStg(stgNow);//��ʼִ�в���
		if(LastAquired>300){
			GridTerminalSystem.GetBlocksOfType<IMyRadioAntenna>(list);
			if(list.Count>0)list[0].SetCustomName("Target Lost");
		}
		if(list.Count>1)list[1].SetCustomName(debugcount.ToString());
		debugcount++;
	}catch(Exception e){
		GridTerminalSystem.GetBlocksOfType<IMyRadioAntenna>(list);
		if(list.Count>0)list[0].SetCustomName(e.ToString());
		nextTick();
	}
}

//��鿪ʼ

//�������
bool MannualWeapon=true;
bool AutoWeapon=false;
private bool WeaponCheck()
{
	//TODO
	MannualWeapon=true;
	AutoWeapon=false;
	return true;
}
/*
Vector3D MyHome = new Vector3D(0, 0, 0);  
private long Home(Vector3D home){
	//TODO
	return 0;
}
*/


//�����Լ�
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

//Ŀ���ȡ
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

//̬�Ƹ�֪����

//���Բ��ֿ�ʼ

//����
enum Strategys { Stasis, Return, QuickApproach, ApproachWithDodge, KeepDistance, LeaveWithDodge, Dodge, Dock, Suicide };
Strategys stgNow=Strategys.Stasis;
Strategys stgLast=Strategys.Stasis;
//�ܿ�
void handleStg(Strategys stg){
	stgLast=stgNow;
	if(InDanger())stgNow=Strategys.Dodge;
	else if(TargetDistance > DstSafe * 3 && LastAquired<300)stgNow=Strategys.QuickApproach;
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
		case Strategys.ApproachWithDodge:
			Strategy_ApproachWithDodge();
			break;
		case Strategys.KeepDistance:
			Strategy_KeepDistance();
			break;
		case Strategys.LeaveWithDodge:
			Strategy_LeaveWithDodge();
			break;
		case Strategys.Dodge:
			Strategy_Dodge();
			break;
		case Strategys.Dock:
			break;
		case Strategys.Suicide:
			break;
	}
	//remote.SetAutoPilotEnabled(true);    
}

bool InDanger(){
	//����Σ�����
	if(
		(TargetSpeedPerTick*60).Length()>30
		&&Vector3D.Dot(Vector3D.Normalize(TargetSpeedPerTick),Vector3D.Normalize(MyPos-Target))>0.85
		&&TargetDistance < DstSafe*2
	)return true;		//Being Chased
	else return false;
}

private void Strategy_ReturnOrigin(){
	Gear(10000000.0f);
	FaceTo(origin, "ReturningOrigin");  
	//5 km ��ȫ���߾���
	if (Vector3D.Distance(Target, remote.GetPosition ()) > 5000)handleStg(Strategys.Stasis);
}

private void Strategy_Stasis(){
	if(Engage){Stasis(false);handleStg(Strategys.QuickApproach);}
	else Stasis(true);
}

Vector3D dodge;
private void Strategy_ApproachWithDodge(){
	Gear(10000000.0f);
	if(TargetDistance>=DstDodge){
		handleStg(Strategys.QuickApproach);
	}else if(TargetDistance<DstFire){
		handleStg(Strategys.KeepDistance);
	}else{
		Vector3D ApproachDirection = Target - MyPos;
		dodge = CalcNextDodgeMove(ApproachDirection,dodge,0.8,0.8);
		FaceTo(dodge, "ApproachingWithDodge");    
	}
}
private void Strategy_QuickApproach(){
	Gear(10000000.0f);
	if(TargetDistance<DstDodge){
		handleStg(Strategys.ApproachWithDodge);
	}else{
		FaceTo(Target+TargetSpeedPerTick*60, "QuickApproaching");
	}
}

int count=0;
int sign=1;
private void Strategy_KeepDistance(){
	Gear(10000000.0f);
	Echo("Attacking");
	GatlingFire();
	Vector3D pos=new Vector3D(0,0,0);
	Vector3D DstKeepV=(TargetDistance<MinAttackDst||TargetDistance>MaxAttackDst)?Vector3D.Normalize((Target-MyPos)*(TargetDistance-(MinAttackDst+MaxAttackDst)/2))*200:new Vector3D(0,0,0);
	if(Vector3D.Distance(Target, remote.GetPosition ()) > DstFire){
		handleStg(Strategys.QuickApproach);
	}else{
		Random ran=new Random();
		if(TimePast%60==0)sign=(ran.Next(-50,50)<0)?sign*-1:sign;
		FaceTo(MyPos+DstKeepV+Vector3D.Normalize(Vector3D.Cross(MySpeed,MyPos-Target)*sign)*500+RandomMove()*0.5, "KeepingDistance");
	}
	//next tick
	nextTick();
}


private void Strategy_LeaveWithDodge(){
	Gear(10000000.0f);
	GatlingFire();
	if(TargetDistance>DstSafe){
		handleStg(Strategys.ApproachWithDodge);
	}else{
		Vector3D FleeDirection = MyPos - Target;   
		dodge = CalcNextDodgeMove(FleeDirection,dodge);
		FaceTo(dodge, "LeavingWithDodge");    
	}
}


//��������
Vector3D edodge=new Vector3D(0,0,0);
private void Strategy_Dodge(){
	Gear(10000000.0f);
	GatlingFire();
	if(TargetDistance>DstSafe*2){
		handleStg(Strategys.ApproachWithDodge);
	}
	else if(TargetDistance>DstSafe||TargetSpeedPerTick.Length()==0){
		Gear(-10000000.0f);
		FaceTo(Target+RandomMove()*0.2, "EmergancyDodge");  
	}else{
		Vector3D MyTargetAcc=(TargetAcceleration.Length()<1)?new Vector3D(1,0,0):TargetAcceleration;
		edodge = Vector3D.Normalize(Vector3D.Cross(TargetSpeedPerTick,MyTargetAcc))*400;
		FaceTo(MyPos+edodge+RandomMove()*0.5, "EmergancyDodge");    
	}
}
//���Բ��ֽ���


//���ܲ��ֿ�ʼ

//�����㷨
private Vector3D CalcNextDodgeMove(Vector3D DirectionTo,Vector3D DirectionNow){
	return CalcNextDodgeMove(DirectionTo,DirectionNow,0.8,0.5);
}
private Vector3D CalcNextDodgeMove(Vector3D DirectionTo,Vector3D DirectionNow,double SpeedShift,double DirectionShift){
	Vector3D Shift=DirectionNow-MyPos;
	Random ran=new Random(); 
	Echo("Dodging?");
	int poscount=0;
	Shift=new Vector3D(ran.Next(GShift_L,GShift_H),ran.Next(GShift_L,GShift_H), ran.Next(GShift_L,GShift_H));
	while(poscount<10&&Vector3D.Dot(Vector3D.Normalize(MySpeed),Vector3D.Normalize(Shift)) > SpeedShift || Vector3D.Dot(Vector3D.Normalize(DirectionTo),Vector3D.Normalize(Shift)) < DirectionShift){
		Shift=new Vector3D(ran.Next(GShift_L,GShift_H),ran.Next(GShift_L,GShift_H), ran.Next(GShift_L,GShift_H));
		Echo("Dodging");
		poscount++;
	}
	return MyPos+Vector3D.Normalize(Shift)*500;
}

Vector3D r60=new Vector3D(0,0,0);
Vector3D r30=new Vector3D(0,0,0);
Vector3D r10=new Vector3D(0,0,0);
private Vector3D RandomMove(){
	Random ran=new Random(); 
	if(TimePast%61==0)r60=new Vector3D(ran.Next(GShift_L,GShift_H),ran.Next(GShift_L,GShift_H), ran.Next(GShift_L,GShift_H));
	if(TimePast%31==0)r30=new Vector3D(ran.Next(GShift_L,GShift_H),ran.Next(GShift_L,GShift_H), ran.Next(GShift_L,GShift_H));
	if(TimePast%11==0)r10=new Vector3D(ran.Next(GShift_L,GShift_H),ran.Next(GShift_L,GShift_H), ran.Next(GShift_L,GShift_H));
	return r60*0.1+r30+0.2+r10*0.3+new Vector3D(ran.Next(GShift_L,GShift_H),ran.Next(GShift_L,GShift_H), ran.Next(GShift_L,GShift_H))*0.4;
}


//��������
private bool ManualShot()
{
	GridTerminalSystem.GetBlocksOfType<IMyUserControllableGun>(list);   
	for(int i=0;i<list.Count;i++)   
	{ 
		IMyUserControllableGun GG = list[i] as IMyUserControllableGun; 
		GG.GetActionWithName("ShootOnce").Apply(GG);
	}
	return true;
}

Dictionary<IMyLargeTurretBase,Vector3D> turretSpeed=new Dictionary<IMyLargeTurretBase,Vector3D>();

void GatlingFire(){
	GridTerminalSystem.GetBlocksOfType<IMyLargeGatlingTurret>(list);   
	for(int i=0;i<list.Count;i++)   
	{ 
		IMyLargeTurretBase gun = list[i] as IMyLargeTurretBase; 
		Vector3D turretAcc=MyAcceleration;
		if(turretSpeed.ContainsKey(gun))turretAcc=turretSpeed[gun]-gun.GetPosition();
		turretSpeed[gun]=gun.GetPosition();
		Vector3D HitPosition = HitPointCaculate(gun.GetPosition(), MySpeed, turretAcc, Target, TargetSpeedPerTick*60, TargetAcceleration*60, 400, 1, 1.55, false); 
		TurretShoot(gun,HitPosition);
	}
}

bool TurretShoot(IMyLargeTurretBase gun,Vector3D pos){
	double a,e;
	MatrixD refLookAtMatrix = MatrixD.CreateLookAt(new Vector3D(0,0,0), gun.WorldMatrix.Forward, gun.WorldMatrix.Up);
	Vector3D targetVector = Vector3D.Normalize(Vector3D.TransformNormal(pos - gun.GetPosition(), refLookAtMatrix));//�ó�Ŀ�귽λ��λʸ��
	Vector3D.GetAzimuthAndElevation(targetVector,out a,out e);
	if(e>-0.05){
		gun.Azimuth=(float)a;
		gun.Elevation=(float)e;
		gun.GetActionWithName("ShootOnce").Apply(gun);
		return true;
	}else{
		return false;
	}
}

enum HPCVersion {V3,V4,V5};
HPCVersion hpcV=HPCVersion.V4;
Vector3D HitPointCaculate(Vector3D Me_Position, Vector3D Me_Velocity, Vector3D Me_Acceleration, Vector3D Target_Position, Vector3D Target_Velocity, Vector3D Target_Acceleration, double Bullet_Speed, double Velocity_Factor, double Acceleration_Factor, bool AbsolutelyBullet)
{
	Vector3D HitPos=new Vector3D(0,0,0);
	switch(hpcV){
		case HPCVersion.V3:
			HitPos = HitPointCaculateV3(Me_Position, Me_Velocity, Me_Acceleration, Target_Position, Target_Velocity, Target_Acceleration, Bullet_Speed, Velocity_Factor, Acceleration_Factor, AbsolutelyBullet);
		break;
		case HPCVersion.V4:
			HitPos = HitPointCaculateV4(Me_Position, Me_Velocity, Me_Acceleration, Target_Position, Target_Velocity, Target_Acceleration, Bullet_Speed, AbsolutelyBullet);
		break;
		case HPCVersion.V5:
			HitPos = HitPointCaculateV5(Me_Position, Me_Velocity, Me_Acceleration, Target_Position, Target_Velocity, Target_Acceleration, Bullet_Speed, AbsolutelyBullet);
		break;
	}
	return HitPos;
}

//���㷨v3.0
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
	
	//��αƽ�����
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


//���㷨����д�档v4.0
//������ײ��ĺ������ֱ��룺�ɴ����ꡢ�ɴ��ٶȡ��ɴ����ٶȡ�Ŀ�����ꡢĿ���ٶȡ�Ŀ����ٶȡ��ӵ��ٶȡ��Ƿ��Ǿ����ӵ��������Լ��ɴ��ٶ�Ӱ��켣���ӵ��Ǿ����ӵ��������ֵ�ʵ���ӵ������ǷǾ����ӵ���
//���һ��bool�������ж��ӵ��Ƿ������Լ��ɴ��ٶ�Ӱ�죬��������ͷ����
Vector3D HitPointCaculateV4(Vector3D Me_Position, Vector3D Me_Velocity, Vector3D Me_Acceleration, Vector3D Target_Position, Vector3D Target_Velocity, Vector3D Target_Acceleration, double Bullet_Speed, bool AbsolutelyBullet)
{
	//�㷨������
	//��������ʸ���������ָ����ײ��Smh = �ӵ��ٶ�*ʱ�䡢 �����ָ��Ŀ�굱ǰλ��Smt = Ŀ������ - ��������ꡢ Ŀ�굱ǰλ��ָ��Ԥ����ײ��Sth = Ŀ���ٶ�*ʱ��
	//������ײ����ڣ��ɵ� Smh = Smt + Sth
	//��һ����дΪ Smt = (Vm - Vt)*t������Vm���ӵ��ٶ�ʸ����Vt��Ŀ���ٶ�ʸ����t�Ǵӷ��䵽��ײ����ʱ�䡣
	//��������ɵ� Vm = Smt/t + Vt������Vm���ӵ��ٶ�ʸ����Smt��Vt����ʸ�������ݹ���ɽ���Ϊ Vm.X = Smt.X/t + Vt.X����Y��Z��ֵͬ��
	//�ּ����ӵ��ٶȵ�ģ��֪���ɵ� Vm.X^2 + Vm.Y^2 + Vm.Z^2 = �ӵ��ٶ�^2��������ģ����ʽ
	//�������еĽ��������һ�й�ʽ�ɵ�
	// (Smt.X/t + Vt.X)^2 + (Smt.Y/t + Vt.Y)^2 + (Smt.Z/t + Vt.Z)^2 = �ӵ��ٶ�^2
	// �� T = 1/t���������
	// T^2(Smt.X^2 + Smt.Y^2 + Smt.Z^2) + 2*T*(Smx.X*Vt.X + Smx.Y*Vt.Y + Smx.Z*Vt.Z) + (Vt.X^2 + Vt.Y^2 + Vt.Z^2) - �ӵ��ٶ�^2 = 0
	// ��һԪ���η���ͨ���֪
	// a = (Smt.X^2 + Smt.Y^2 + Smt.Z^2)
	// b = 2*(Smx.X*Vt.X + Smx.Y*Vt.Y + Smx.Z*Vt.Z)
	// c = (Vt.X^2 + Vt.Y^2 + Vt.Z^2) - �ӵ��ٶ�^2
	// ����� b^2 - 4ac >= 0ʱ�н⣬������������ײ�����������������ֵ����0ʱ���ӵ��ٶȱ�Ȼ����Ŀ��ľ���Զ���ٶȷ���
	// ��� T = (-b +- (b^2 - 4ac)^0.5) / 2*a
	// ��ȥ��ֵ���ɵõ������ t = 1/T
	
	Vector3D Smt = Target_Position - Me_Position;
	double a = Math.Pow(Smt.X,2) + Math.Pow(Smt.Y,2) + Math.Pow(Smt.Z,2);
	double b = 2*(Smt.X*Target_Velocity.X + Smt.Y*Target_Velocity.Y + Smt.Z*Target_Velocity.Z);
	double c = Math.Pow(Target_Velocity.X,2) + Math.Pow(Target_Velocity.Y,2) + Math.Pow(Target_Velocity.Z,2) - Math.Pow(Bullet_Speed,2);
	
	double bac = Math.Pow(b,2) - (4*a*c);
	
	double TimeToHit_Z = 1/((-b + Math.Pow(bac,0.5))/(2*a));
	double TimeToHit_F = 1/((-b - Math.Pow(bac,0.5))/(2*a));
	
	Vector3D MeMoveEffect = new Vector3D();
	if(!AbsolutelyBullet)
	{
		MeMoveEffect = TimeToHit_Z*(Me_Velocity + 0.5*Me_Acceleration*TimeToHit_Z);
	}
	
	Vector3D HitPosition = Target_Position + TimeToHit_Z*(Target_Velocity + 0.5*Target_Acceleration*TimeToHit_Z) - MeMoveEffect;
	
	return HitPosition;
}

//���㷨��һ���Ż���v5.0
//������ײ��ĺ������ֱ��룺�ɴ����ꡢ�ɴ��ٶȡ��ɴ����ٶȡ�Ŀ�����ꡢĿ���ٶȡ�Ŀ����ٶȡ��ӵ��ٶȡ��Ƿ��Ǿ����ӵ��������Լ��ɴ��ٶ�Ӱ��켣���ӵ��Ǿ����ӵ��������ֵ�ʵ���ӵ������ǷǾ����ӵ���
//���һ��bool�������ж��ӵ��Ƿ������Լ��ɴ��ٶ�Ӱ�죬��������ͷ����
Vector3D HitPointCaculateV5(Vector3D Me_Position, Vector3D Me_Velocity, Vector3D Me_Acceleration, Vector3D Target_Position, Vector3D Target_Velocity, Vector3D Target_Acceleration, double Bullet_Speed, bool AbsolutelyBullet)
{
	//��4.0�汾�Ļ����ϣ��Ƚ��д���ʱ��ļ��㣬�������������ʱ������Ŀ���ٶȣ������Լ��ٶȡ����ٶȼ�Ŀ����ٶȣ����ٸ����������Ŀ���ٶȽ��м�����̣�Ȼ�������㾫ȷʱ�䡣
	//�÷�����ͨ���Ƚϴ���ʱ��roughTime��׼ȷʱ��TimeToHit_Z��ֵ���ж��޽����ʵ��


	double roughTime = Vector3D.Distance(Target_Position, Me_Position)/Bullet_Speed; //����ʱ��
	if(!AbsolutelyBullet) //���Լ��ٶȽ����������ó�����ƽ���ٶ�
	{
		Target_Velocity += -Me_Velocity + 0.5*Target_Acceleration*roughTime - 0.5*Me_Acceleration*roughTime;
	}
	else //��Ŀ���ٶȽ��������ó�����ƽ���ٶ�
	{
		Target_Velocity += 0.5*Target_Acceleration*roughTime;
	}

	Vector3D Smt = Target_Position - Me_Position;
	double a = Math.Pow(Smt.X,2) + Math.Pow(Smt.Y,2) + Math.Pow(Smt.Z,2);
	double b = 2*(Smt.X*Target_Velocity.X + Smt.Y*Target_Velocity.Y + Smt.Z*Target_Velocity.Z);
	double c = Math.Pow(Target_Velocity.X,2) + Math.Pow(Target_Velocity.Y,2) + Math.Pow(Target_Velocity.Z,2) - Math.Pow(Bullet_Speed,2);

	double bac = Math.Pow(b,2) - (4*a*c);

	double TimeToHit_Z = 1/((-b + Math.Pow(bac,0.5))/(2*a));
	double TimeToHit_F = 1/((-b - Math.Pow(bac,0.5))/(2*a));


	Vector3D HitPosition = Target_Position + TimeToHit_Z*Target_Velocity; //�����ٶ��������ƽ���ٶ�

	return HitPosition;
}

//��ʱ��Ƶ�ʿ���
private void nextTick()
{
	timer.GetActionWithName("TriggerNow").Apply(timer);
	Ticking=true;
}
private void Stasis(bool status)
{
	if(status)
	{
		//��������Ƶ��
		if(timer != null)timer.SetValue("TriggerDelay",SleepFrq);
		//Խ�����ƽ���
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
		//����ս��Ƶ��
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

//��׼�������
enum FaceModes {Remote,Gyro};
FaceModes FaceMode=FaceModes.Gyro;
int TimePast=0;
void FaceTo(Vector3D pos,string description)
{
	switch(FaceMode){
		case FaceModes.Gyro:
		Vector3D targetposition = pos;
		MatrixD refLookAtMatrix = MatrixD.CreateLookAt(new Vector3D(0,0,0), remote.WorldMatrix.Forward, remote.WorldMatrix.Up);
		Vector3D targetVector = Vector3D.Normalize(Vector3D.TransformNormal(targetposition - remote.GetPosition(), refLookAtMatrix));//�ó�Ŀ�귽λ��λʸ��
		//������Ŀ�����Remote�Ĺ�һ��������X�����ң���-��+��Y�����£���-��+
		//����ӦΪ�������������ֻ��������ת�򼴿ɣ������ǵ�Yaw�ǿ������ң���-��+
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


//����Ϊ��������

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


//�����������ǿ���
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


//����
List<IMyTerminalBlock> Gyroscopes;

//�����������
string[] gyroYawField = null;
string[] gyroPitchField = null;
string[] gyroRollField = null;
float[] gyroYawFactor = null;
float[] gyroPitchFactor = null;
float[] gyroRollFactor = null;

//��λ�������
MatrixD refWorldMatrix; //��ֻ����
const float GYRO_FACTOR = (float)(Math.PI / 30);
Vector3D Y_VECTOR = new Vector3D(0, -1, 0);
Vector3D Z_VECTOR = new Vector3D(0, 0, -1);
Vector3D POINT_ZERO = new Vector3D(0, 0, 0);

bool GyroInited = false;
void GyroInit(IMyRemoteControl MyRemote)
{
	//����������
	refWorldMatrix=MyRemote.WorldMatrix;
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
//���ܲ��ֽ���