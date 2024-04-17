namespace WebSocketsSample.Model
{
    public class UpdatableObjects
    {
        public uint Id { get; set; }
        public required string Type { get; set; }
        public dynamic? Data { get; set; }
    }
}
