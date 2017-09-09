//无人机火控

List<IMyTerminalBlock> list = new List<IMyTerminalBlock>();  
int t=0;
void Main(string argument)
{
	GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(list); 
	IMyRemoteControl remote = list[1] as IMyRemoteControl;
	Vector3D pos=new Vector3D(0,0,0);
	remote.GetNearestPlayer(out pos); 
	//var ant = GridTerminalSystem.GetBlockWithName("Antenna") as IMyRadioAntenna;
	//ant.TransmitMessage(pos.ToString(), MyTransmitTarget.Everyone);
	Echo(pos.ToString());
	Echo(t.ToString());
	t++;
}