

#region SELECT MODE

using P2PTransfer;

var mode = string.Empty;


do
{
    Console.Write($"Receive:1 OR Send:2 : ");
    mode = Console.ReadLine();
}
while (mode != "1" && mode != "2");

#endregion

#region SETUP CONNECTION

try
{
    //Receive
    if (mode == "1")
    {
        await new Receiver().Start();

    }
    //Send
    else if (mode == "2")
    {

        await new Sender().Start();

    }

}
finally
{
    Console.WriteLine(string.Empty);
    Console.WriteLine($"Press enter to close");
    Console.ReadLine();
}

#endregion