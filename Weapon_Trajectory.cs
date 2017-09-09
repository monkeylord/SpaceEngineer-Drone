//武器控制
//其中弹道计算部分来自MEA群主的算法

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
	Vector3D targetVector = Vector3D.Normalize(Vector3D.TransformNormal(pos - gun.GetPosition(), refLookAtMatrix));//得出目标方位单位矢量
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


//新算法，重写版。v4.0
//计算碰撞点的函数，分别传入：飞船座标、飞船速度、飞船加速度、目标座标、目标速度、目标加速度、子弹速度、是否是绝对子弹（不受自己飞船速度影响轨迹的子弹是绝对子弹，加特林等实体子弹都是是非绝对子弹）
//最后一个bool变量是判断子弹是否是受自己飞船速度影响，例如摄像头激光
Vector3D HitPointCaculateV4(Vector3D Me_Position, Vector3D Me_Velocity, Vector3D Me_Acceleration, Vector3D Target_Position, Vector3D Target_Velocity, Vector3D Target_Acceleration, double Bullet_Speed, bool AbsolutelyBullet)
{
	//算法解析：
	//构建三个矢量，发射点指向碰撞点Smh = 子弹速度*时间、 发射点指向目标当前位置Smt = 目标座标 - 发射点座标、 目标当前位置指向预计碰撞点Sth = 目标速度*时间
	//假设碰撞点存在，可得 Smh = Smt + Sth
	//进一步可写为 Smt = (Vm - Vt)*t。其中Vm是子弹速度矢量，Vt是目标速度矢量，t是从发射到碰撞所需时间。
	//化简整理可得 Vm = Smt/t + Vt。其中Vm是子弹速度矢量，Smt、Vt均是矢量，根据规则可解析为 Vm.X = Smt.X/t + Vt.X。其Y、Z的值同理
	//又假设子弹速度的模已知，可得 Vm.X^2 + Vm.Y^2 + Vm.Z^2 = 子弹速度^2。即向量模长公式
	//将上上行的结果带入上一行公式可得
	// (Smt.X/t + Vt.X)^2 + (Smt.Y/t + Vt.Y)^2 + (Smt.Z/t + Vt.Z)^2 = 子弹速度^2
	// 令 T = 1/t带入整理得
	// T^2(Smt.X^2 + Smt.Y^2 + Smt.Z^2) + 2*T*(Smx.X*Vt.X + Smx.Y*Vt.Y + Smx.Z*Vt.Z) + (Vt.X^2 + Vt.Y^2 + Vt.Z^2) - 子弹速度^2 = 0
	// 用一元二次方程通解可知
	// a = (Smt.X^2 + Smt.Y^2 + Smt.Z^2)
	// b = 2*(Smx.X*Vt.X + Smx.Y*Vt.Y + Smx.Z*Vt.Z)
	// c = (Vt.X^2 + Vt.Y^2 + Vt.Z^2) - 子弹速度^2
	// 结果当 b^2 - 4ac >= 0时有解，这里隐含了碰撞存在条件，即当这个值大于0时，子弹速度必然大于目标的径向远离速度分量
	// 结果 T = (-b +- (b^2 - 4ac)^0.5) / 2*a
	// 舍去负值即可得到结果， t = 1/T
	
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

//新算法进一步优化版v5.0
//计算碰撞点的函数，分别传入：飞船座标、飞船速度、飞船加速度、目标座标、目标速度、目标加速度、子弹速度、是否是绝对子弹（不受自己飞船速度影响轨迹的子弹是绝对子弹，加特林等实体子弹都是是非绝对子弹）
//最后一个bool变量是判断子弹是否是受自己飞船速度影响，例如摄像头激光
Vector3D HitPointCaculateV5(Vector3D Me_Position, Vector3D Me_Velocity, Vector3D Me_Acceleration, Vector3D Target_Position, Vector3D Target_Velocity, Vector3D Target_Acceleration, double Bullet_Speed, bool AbsolutelyBullet)
{
	//在4.0版本的基础上，先进行粗略时间的计算，进而用这个粗略时间修正目标速度（考虑自己速度、加速度及目标加速度）。再根据修正后的目标速度进行计算过程，然后反馈计算精确时间。
	//该方法可通过比较粗略时间roughTime和准确时间TimeToHit_Z的值来判断修结果真实度


	double roughTime = Vector3D.Distance(Target_Position, Me_Position)/Bullet_Speed; //粗略时间
	if(!AbsolutelyBullet) //将自己速度介入修正，得出粗略平均速度
	{
		Target_Velocity += -Me_Velocity + 0.5*Target_Acceleration*roughTime - 0.5*Me_Acceleration*roughTime;
	}
	else //将目标速度介入修正得出粗略平均速度
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


	Vector3D HitPosition = Target_Position + TimeToHit_Z*Target_Velocity; //经加速度修正后的平均速度

	return HitPosition;
}
