//策略部分开始

//策略
enum Strategys { Stasis, Return, QuickApproach, ApproachWithDodge, KeepDistance, LeaveWithDodge, Dodge, Dock, Suicide };
Strategys stgNow=Strategys.Stasis;
Strategys stgLast=Strategys.Stasis;
//总控
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
	//定义危险情况
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
	//5 km 安全休眠距离
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


//紧急闪避
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
//策略部分结束


//功能部分开始

//闪避算法
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