/*
Fighter Drone V0.1
Designed for DroneCarrier
Coded by Monkeylord
*/

/*
无人机基本逻辑：
Basic Logic:
命令处理 -> 态势感知 -> 策略判断与执行 => 飞控执行
Command Handling -> Sense -> Strategys => Execution
*/
/*开关与设定 Switchs & Prameters*/
enum Commands{Attack,Dock,None};
struct Distances{
	int Safe=2000;
	int Engage=1200;
	int Fire=800;
	int Surround=600;
}
/*运行状态记录 Status*/
String CurrentCommand;
int CmdLU=0;
struct Entity{
	Vector3D Position = new Vector3D(0);
	Vector3D Velocity = new Vector3D(0);
	Vector3D Acceleration = new Vector3D(0);
	int LastUpdate=0;
};
Entity target=new Entity(),myself=new Entity();
int Ticks=0;
IMyShipController controller;

/*态势判断 Judgements*/
bool IsBeingChased=false;
bool IsChasing=false;
bool DoExcution=false;

void Main(string argument){
	CommandHandle(argument);
	Sense();
	Strategys();
	Ticks+=1;
}

void CommandHandle(String argument){
	if(argument!=""){
		string[] cmdArray=argument.Split('#');
		CurrentCommand=cmdArray[0];
		switch(cmdArray[0]){
			case "Attack":
			case "Dock":
		}
		CmdLU=Ticks;
	}else if(Ticks-CmdLU>100){
		CurrentCommand="None";
	}
}
void Sense(){
	//TODO 初始化飞控
	//TODO 自检
	//TODO 态势判断
}
void Strategys(){
	//TODO 不同命令、不同态势的执行策略，这里统一分派，具体由子函数执行
}
















