# SimpleDoubleRatchet

A simple to use, transport agnostic implementation of [The Double Ratchet Algorithm](https://whispersystems.org/docs/specifications/doubleratchet/) created by Open Whisper Systems.

## Demo

	- Alice is the initial sender, Bob is the initial receiver.
	- Alice starts the handshake which gets sent over the already connected transport layer.
	- Bob must wait for a message from Alice before sending his first message.

## Usage
## 1) Opening the channel
First setup some communication channel you want to secure.

**Sender**
```cs
TcpClient SenderTx = new TcpClient(ReceiverIpAddress, ReceiverRXPort);
TcpServer SenderRx = new TcpServer(SenderIpAddress, SenderTXPort);
```
**Receiver**
```cs
TcpClient ReceiverTx = new TcpClient(SenderIpAddress, ReceiverTXPort);
TcpServer ReceiverRx = new TcpServer(ReceiverIpAddress, SenderRXPort);
```

Then create an instance of ```DRChannel``` and connect it to the communication channel.

**Sender**
```cs
DRChannel SenderChannel = new DRChannel(); // or new DRChannel(true) to disable encryption for debugging content
SenderChannel.HandleTransportSend = (string packet) => 
{
    SenderTx.Write(packet);
};
SenderRx.OnPacket = (string packet) => 
{
    SenderChannel.HandleTransportReceive(packet);
};
bool success = SenderChannel.Open(true); // isSender = true, 30 second timeout
```
**Receiver**
```cs
DRChannel ReceiveChannel = new DRChannel(); 
ReceiveChannel.HandleTransportSend = (string packet) => 
{
    ReceiverTx.Write(packet);
};
ReceiverRx.OnPacket = (string packet) => 
{
    ReceiveChannel.HandleTransportReceive(packet);
};
bool success = ReceiverChannel.Open(false); // isSender = false
```

## 2) Using the channel synchronously
Once the channel is opened successfully, the Receiver should wait for the first message and the Sender can send the first message to start the Double Ratchet process. Once the first message is received, the channel is bi-directional. 

**Receiver**
```cs
while(true) 
{
    string message = ReceiverChannel.WaitForResponse();
    if ( message == "ping" ) 
    {
        ReceiverChannel.Send("pong");
    }
    else if ( message == "pong");
    {
        ReceiverChannel.Send("ping");
    }
}
```
**Sender**
```cs
SenderChannel.Send("ping");
while(true) 
{
    string message = SenderChannel.WaitForResponse();
    if ( message == "ping" ) 
    {
        SenderChannel.Send("pong");
    }
    else if ( message == "pong");
    {
        SenderChannel.Send("ping");
    }
}
```

## 3) Using the channel asynchronously
Instead of waiting for the responses individually, you can also provide a delegate to handle the incomming messages

**Receiver**
```cs
ReceiverChannel.OnMessage = (string message) => 
{
    if ( message == "ping" ) 
    {
        ReceiverChannel.Send("pong");
    }
    else if ( message == "pong" ) 
    {
        ReceiverChannel.Send("ping");
    }
};
```
**Sender**
```cs
SenderChannel.OnMessage = (string message) =>
{
    if ( message == "ping" ) 
    {
        SenderChannel.Send("pong");
    }
    else if ( message == "pong" )
    {
        SenderChannel.Send("ping");
    }
};
SenderChannel.Send("ping");
```