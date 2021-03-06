UnionControl uc=null;
public void Main(string argument) {

    // The main entry point of the script, invoked every time
    // one of the programmable block's Run actions are invoked.
    // 
    // The method itself is required, but the argument above
    // can be removed if not needed.
    if(uc==null)uc=new UnionControl(this);
    uc.Update();
}
public class UnionControl{
    
    MyGridProgram MGP;
    IMyShipController SettedMain;    //指定主控
    IMyShipController Main;            //当前主控
    Vector3D MyPos;
    Vector3D MyVelocity;
    Vector3D CruiseVelocity=new Vector3D(0);
    Vector3D CruiseDirection=new Vector3D(0);
    Vector3D faceToDir=new Vector3D(0);
    bool IsCruising;
    bool InertiaDump;
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
    //public void Update();
    //public bool Cruise(bool Cruise);
    //public bool Inertia(bool inertia);
    //public bool Manual(bool OnOff);
    //public void Heading(Vector3D headingTo);
    //public void VectorMove(Vector3D Power,Vector3D headingTo);
    //public void Output(Vector3D OutputVector);
    public UnionControl(MyGridProgram mgp,IMyShipController SC=null){
        MGP=mgp;
        SettedMain=SC;
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
        }
        //TODO    更新当前速度、加速度、旋转状态，更新当前操作状态
        MyPos=Main.GetPosition();
        MyVelocity=Main.GetShipVelocities().LinearVelocity;
        InertiaDump=Main.DampenersOverride;
        Vector3 MoveIndicator=Main.MoveIndicator;
        MatrixD refLookAtMatrix = MatrixD.CreateLookAt(new Vector3D(0,0,0), Main.WorldMatrix.Forward, Main.WorldMatrix.Up);
        Vector3D rotation = (ManualControl)?Gyro(faceToDir):new Vector3D(Main.RotationIndicator.Y,-Main.RotationIndicator.X,-Main.RollIndicator*100);        //陀螺仪方向
        //控制陀螺仪惯性
        Vector3D AngularVectorToMe = Vector3D.TransformNormal(Main.GetShipVelocities().AngularVelocity, refLookAtMatrix); 
        rotation = rotation - new Vector3D(AngularVectorToMe.Y,-AngularVectorToMe.X,-AngularVectorToMe.Z);
        SetGyroYPR(rotation);
        //将飞船速度转换坐标系为相对速度

        Vector3D relativeVelocity = new Vector3D(0);
        if(MyVelocity.Length()>0)relativeVelocity = Vector3D.TransformNormal(MyVelocity, refLookAtMatrix);    //坐标系转换后的相对速度
        Vector3D relativeCruiseVelocity = new Vector3D(0);
        if(CruiseVelocity.Length()>0)relativeCruiseVelocity = Vector3D.TransformNormal(CruiseVelocity, refLookAtMatrix);
        //判断惯性控制输出
        Vector3D output=(ManualControl)?OutputVector:new Vector3D(MoveIndicator.X*-100f,MoveIndicator.Y*-100f,MoveIndicator.Z*-100f);// 从控制到出力转换
        if(InertiaDump){
            if(output.X==0)output.X=CalcInertiaDump(relativeVelocity.X-relativeCruiseVelocity.X);
            if(output.Y==0)output.Y=CalcInertiaDump(relativeVelocity.Y-relativeCruiseVelocity.Y);
            if(output.Z==0)output.Z=CalcInertiaDump(relativeVelocity.Z-relativeCruiseVelocity.Z);
        }
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
        OutputVector=outputVector;
    }
    public void VectorMove(Vector3D Power,Vector3D headingTo){
        //TODO    根据推进器最大出力，将方向矢量转换为出力矢量。
    }
    private void GEngine(Vector3D output){
        //TODO    六向重力控制
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
        //TODO    六向常规推力
        foreach(IMyThrust Thrust in Thrusts){
            switch(Directions[Thrust]){
                case Base6Directions.Direction.Forward:
                    Thrust.SetValue("Override",Convert.ToSingle((output.Z>0)?output.Z:0f));
                break;
                case Base6Directions.Direction.Backward:
                    Thrust.SetValue("Override",Convert.ToSingle((output.Z<0)?-output.Z:0f));
                break;
                case Base6Directions.Direction.Left:
                    Thrust.SetValue("Override",Convert.ToSingle((output.X>0)?output.X:0f));
                break;
                case Base6Directions.Direction.Right:
                    Thrust.SetValue("Override",Convert.ToSingle((output.X<0)?-output.X:0f));
                break;
                case Base6Directions.Direction.Down:
                    Thrust.SetValue("Override",Convert.ToSingle((output.Y>0)?output.Y:0f));
                break;
                case Base6Directions.Direction.Up:
                    Thrust.SetValue("Override",Convert.ToSingle((output.Y<0)?-output.Y:0f));
                break;
            }
        }
    }
    private void SEngine(Vector3D output){
        //TODO    非常规推力
    }
    Vector3D Keep=new Vector3D(0);
    private Vector3D Gyro(Vector3D faceToDir){
        //TODO    航向保持
        MatrixD refLookAtMatrix = MatrixD.CreateLookAt(new Vector3D(0,0,0), Main.WorldMatrix.Forward, Main.WorldMatrix.Up);
        Vector3D indicator=Vector3D.Normalize(Vector3D.TransformNormal(faceToDir, refLookAtMatrix))*40;
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
        //TODO    六向重力引擎统计
        GGs.Clear();
        MGP.GridTerminalSystem.GetBlocksOfType<IMyGravityGenerator> (GGs);
        foreach(IMyGravityGenerator GG in GGs){
            Directions.Add(GG,refWorldMatrix.GetClosestDirection(GG.WorldMatrix.Down));
        }
        //TODO    六向推进器统计
        Thrusts.Clear();
        MGP.GridTerminalSystem.GetBlocksOfType<IMyThrust> (Thrusts);
        foreach(IMyThrust Thrust in Thrusts){
            Directions.Add(Thrust,refWorldMatrix.GetClosestDirection(Thrust.WorldMatrix.Backward));
        }
        //TODO 统一处理方向
        

    }
    private float CalcInertiaDump(double Speed){
        if(Speed>1)return 100f;
        else if(Speed<-1)return -100f;
        else if(Math.Abs(Speed)<0.001)return 0f;
        else return (float)(Speed*Speed*Speed)*100;
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