package server

type ActorPositionInfo struct {
	ActorID    string `json:"actor_id"`
	ActorType  string `json:"actor_type"`
	Domain     string `json:"domain"`
	TTL        int64  `json:"ttl"`
	CreateTime int64  `json:"create_time"`
	ServerID   int64  `json:"server_id"`
}

func (this *ActorMembership) findPosition() {

}
