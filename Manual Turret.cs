List<IMyTerminalBlock> list = new List<IMyTerminalBlock>();
List<IMyTerminalBlock> listT = new List<IMyTerminalBlock>();
public void Main(string argument) {
	GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(list);
	IMyRemoteControl remote=list[0] as IMyRemoteControl;
	GridTerminalSystem.GetBlocksOfType<IMyLargeTurretBase>(list);
	IMyLargeTurretBase gun=list[0] as IMyLargeTurretBase;
	
	Vector3D pos,pos2;
	remote.GetNearestPlayer(out pos);
	
	GunShoot(gun,pos);
}

bool GunShoot(IMyLargeTurretBase gun,Vector3D pos){
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