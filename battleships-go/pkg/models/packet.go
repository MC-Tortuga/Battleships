package models

type IPacket interface {
	GetType() PacketType
}
