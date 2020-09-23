![Icon](Icon.png)
# ToastedNetwork
A wrapper around Lidgren.Network allowing to send packet objects. Designed to be Event-based for games and applications

# How it works
All packets inherit from IPacket (DataPacket, RequestPacket, ResponsePacket). 
These packets are then serialized using `IPacket.Serialize(long SenderIdentifier)`, and are then send over Lidgren.Network Client / Server and Deserialized at the receiving end.

# License
This project includes `Lidgren.Network` and `Polenter.Serialization`. Use `ToastedNetwork` however you want it, but add credit.
