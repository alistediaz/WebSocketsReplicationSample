using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebSocketsSample.Model;

namespace WebSocketsSample.Controllers;

#region snippet_Controller_Connect
public class WebSocketController(ConcurrentDictionary<uint, StaticObjects> staticObjectsManager,
                                 ConcurrentDictionary<uint, UpdatableObjects> updatableObjectsManager,
                                 ConcurrentDictionary<uint, WebSocket> connections) : ControllerBase
{
    private static WebSocket? ws;
    private const string ReplicationPackageType = "2";


   [Route("/ws")]
    public async Task Get()
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            await NetworkObjectsProcessor(webSocket, updatableObjectsManager, connections);
        }
        else
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }
    #endregion

    private static async Task NetworkObjectsProcessor(WebSocket webSocket, ConcurrentDictionary<uint, UpdatableObjects> updatableObjectsManager, ConcurrentDictionary<uint, WebSocket> connections)
    {
        var buffer = new byte[1024];
        var receiveResult = await webSocket.ReceiveAsync(
            new ArraySegment<byte>(buffer), CancellationToken.None);
        var strObject = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
        var (isSuccess, objectResult) = extractJson(strObject);
        
        if (isSuccess)
        {
            uint networkObjectId = ((UpdatableObjects)objectResult).Id;


            if (!connections.ContainsKey(networkObjectId))
            {
                connections.TryAdd(networkObjectId, webSocket);
                await SendUpdatableObjects(webSocket, updatableObjectsManager, receiveResult, objectResult);
            }

            while (true)
            {
                if (isSuccess)
                {
                    string serializeObjectPackage = "[" + ReplicationPackageType + "," + strObject + "]";
                    var bytesToSend = Encoding.UTF8.GetBytes(serializeObjectPackage);
                    ArraySegment<byte> arraySegment = new(bytesToSend);

                    foreach (var item in connections)
                    {
                        if (item.Value.State != WebSocketState.Open || item.Key == networkObjectId)
                        {
                            continue;
                        }

                        await item.Value.SendAsync(
                        arraySegment,
                        receiveResult.MessageType,
                        receiveResult.EndOfMessage,
                        CancellationToken.None);
                    }
                }

                receiveResult = await webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer), CancellationToken.None);

                if (receiveResult.CloseStatus.HasValue)
                {
                    connections.TryRemove(networkObjectId, out ws);
                    await webSocket.CloseAsync(
                    receiveResult.CloseStatus.Value,
                    receiveResult.CloseStatusDescription,
                    CancellationToken.None);
                    break;
                }

                (isSuccess, objectResult) = extractJson(Encoding.UTF8.GetString(buffer, 0, receiveResult.Count));
                networkObjectId = ((UpdatableObjects)objectResult).Id;

                if (isSuccess && updatableObjectsManager.ContainsKey(networkObjectId)){
                    string oldObject = JsonConvert.SerializeObject(updatableObjectsManager[networkObjectId].Data!);
                    string newObject = JsonConvert.SerializeObject(objectResult.Data);

                    string mergeStrings = oldObject[..^1] + "," + newObject[1..];
                    updatableObjectsManager[networkObjectId].Data = JsonConvert.DeserializeObject<dynamic>(mergeStrings)!;
                }

                strObject = JsonConvert.SerializeObject(objectResult);
            }
        }

        static (bool, dynamic) extractJson(string strJson)
        {
            try
            {
                 var updatableObjects = JsonConvert.DeserializeObject<UpdatableObjects>(strJson)!;

                if (updatableObjects != null)
                {
                    return (true, updatableObjects);
                } else 
                { 
                    return (false, JsonConvert.DeserializeObject<dynamic>("{\"error\":\"Conversión retornó null\"}")!); 
                }

            }
            catch (Exception ex)
            {
                return (false, JsonConvert.DeserializeObject<dynamic>("{\"error\":\"" + ex.Message + "\"}")!);
            }
        }

        static async Task SendUpdatableObjects(WebSocket webSocket, ConcurrentDictionary<uint, UpdatableObjects> updatableObjectsManager, WebSocketReceiveResult receiveResult, dynamic objectResult)
        {
            uint networkObjectId = ((UpdatableObjects)objectResult).Id;
            updatableObjectsManager.TryAdd(networkObjectId, objectResult);
            string serializedUpdatableObjectsManager = "[" + ReplicationPackageType + "," + BuildSerialize(updatableObjectsManager) + "]";
            var bytesToSend = Encoding.UTF8.GetBytes(serializedUpdatableObjectsManager);
            ArraySegment<byte> arraySegment = new(bytesToSend);

            await webSocket.SendAsync(
                arraySegment,
                receiveResult.MessageType,
                receiveResult.EndOfMessage,
                CancellationToken.None);
        }

        static string BuildSerialize(ConcurrentDictionary<uint, UpdatableObjects> updatableObjectsManager)
        {
            StringBuilder sb = new StringBuilder();

            foreach(KeyValuePair<uint, UpdatableObjects> updatableObject in updatableObjectsManager)
            {
                sb.Append(JsonConvert.SerializeObject(updatableObject.Value) + ",");
            }

            return sb.ToString();
        }
    }
}
