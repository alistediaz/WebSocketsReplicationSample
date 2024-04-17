namespace WebSocketsSample.Model
{
    public class StaticObjects(string type, Position position)
    {
        public required string Type {  get; set; } = type;
        public required Position Position { get; set; } = position;
        public string? TargetRoomId { get; set; }
        public string? TargetSpawnPointId { get; set; }
        public string? Id { get; set; }
        public string? SpawnDirection { get; set; }


    }
}
