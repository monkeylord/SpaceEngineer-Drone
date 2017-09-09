public class UnionControl{
	IMyShipController SettedMain;	//指定主控
	IMyShipController Main;			//当前主控
	Vector3D MyPos;
	Vector3D MyVelocity;
	Vector3D CruiseVelocity=new Vector3D(0);
	Vector3D CruiseDirection=new Vector3D(0);;
	bool IsCruising;
	bool InertiaDump;
	String LastMsg;
	//加速、转向、翻滚、惯性参数
	
	public UnionControl(IMyShipController SC){
		SettedMain=SC;
		Update();
	}
	public void Update(){
		//TODO	如果主控未设置则寻找主控，如果主控有变化，则重新初始化
		if(SettedMain==null){
			/*	TODO	寻找主控，并且处于操控状态*/
			if(Main!=MainFound && MainFound!=null)Init();
		}
		//TODO	更新当前速度、加速度、旋转状态，更新当前操作状态
		Vector3D MoveIndictor;	//当前控制加速状态
		Vector3D faceToDir;		//陀螺仪方向
		//将飞船速度转换坐标系为相对速度
		Vector3D relativeVelocity;	//坐标系转换后的相对速度
		Vector3D relativeCruiseVelocity;
		//判断惯性控制输出
		if(InertiaDump){
			if(MoveIndictor.X==0)MoveIndictor.X=CalcInertiaDump(relativeVelocity.X-relativeCruiseVelocity.X);
			if(MoveIndictor.Y==0)MoveIndictor.Y=CalcInertiaDump(relativeVelocity.Y-relativeCruiseVelocity.Y);
			if(MoveIndictor.Z==0)MoveIndictor.Z=CalcInertiaDump(relativeVelocity.Z-relativeCruiseVelocity.Z);
		}
		output=MoveIndictor;//TODO 控制到出力转换
		GEngine(output);
		TEngine(output);
		SEngine(output);
		Gyro(faceToDir);
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
	public bool InertiaDump(bool inertiaDump){
		InertiaDump=inertiaDump;
		LastMsg="InertiaDump is Setted on "+InertiaDump;
		return InertiaDump;
	}
	private void GEngine(Vector3D output){
		//TODO	六向重力控制
		foreach(GG in GGs){
			switch(Directions[GG]){
				case front:
				/*TODO	设置出力，如果出力为0则关闭	GG.Force=output.x*/
				break;
				//TODO	完成更多方向推进
			}
		}
	}
	private void TEngine(Vector3D output){
		//TODO	六向常规推力
	}
	private void SEngine(Vector3D output){
		//TODO	非常规推力
	}
	private void Gyro(Vector3D faceTo){
		//TODO	陀螺仪指向
	}
	private void Init(){
		//TODO	六向陀螺仪统计
		//TODO	六向重力引擎统计
		//TODO	六向推进器统计
	}
	private float CalcInertiaDump(float Speed){
		//TODO	指数减
		return Speed;
	}
}