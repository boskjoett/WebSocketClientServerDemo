using System;
using System.Windows;
using System.Net.WebSockets;
using System.Threading;
using System.Text;
using Newtonsoft.Json;
using SharedDataTypes;
using System.Diagnostics;

namespace WebSocketClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ClientWebSocket websocket;
        private bool disconnecting;
        private Guid subscriptionId;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void ButtonConnect_Click(object sender, RoutedEventArgs e)
        {
            disconnecting = false;
            websocket = new ClientWebSocket();

            Uri serverUri = new Uri(TextBoxServerUrl.Text);

            await websocket.ConnectAsync(serverUri, CancellationToken.None);

            if (websocket.State == WebSocketState.Open)
            {
                ProcessIncomingMessages();
            }
        }

        private void ButtonDisconnect_Click(object sender, RoutedEventArgs e)
        {
            disconnecting = true;
            websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnected", CancellationToken.None).Wait();
            websocket.Dispose();
            websocket = null;
        }

        private void ButtonAddSubscriber_Click(object sender, RoutedEventArgs e)
        {
            if (websocket.State != WebSocketState.Open)
            {
                return;
            }

            AddSubscriberRequestMessage request = new AddSubscriberRequestMessage
            {
                SubscriberId = 100,
                Name = "John Doe",
                Topic = "Weather reports"
            };

            string jsonText = JsonConvert.SerializeObject(request);
            byte[] msg = Encoding.UTF8.GetBytes(jsonText);
            websocket.SendAsync(new ArraySegment<byte>(msg), WebSocketMessageType.Text, true, CancellationToken.None).Wait(5000);
        }

        private void ButtonRemoveSubscriber_Click(object sender, RoutedEventArgs e)
        {
            if (websocket.State != WebSocketState.Open)
            {
                return;
            }

            RemoveSubscriberRequestMessage request = new RemoveSubscriberRequestMessage
            {
                SubscriberId = 100,
                SubscriptionId = subscriptionId
            };

            string jsonText = JsonConvert.SerializeObject(request);
            byte[] msg = Encoding.UTF8.GetBytes(jsonText);
            websocket.SendAsync(new ArraySegment<byte>(msg), WebSocketMessageType.Text, true, CancellationToken.None).Wait(5000);
        }

        private async void ProcessIncomingMessages()
        {
            var buffer = new byte[1024];

            try
            {
                while (websocket.State == WebSocketState.Open)
                {
                    var stringResult = new StringBuilder();
                    WebSocketReceiveResult result;

                    do
                    {
                        result = await websocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            if (websocket != null && !disconnecting)
                            {
                                websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by server", CancellationToken.None).Wait();
                                OnClose();
                            }
                            return;
                        }
                        else
                        {
                            var str = Encoding.UTF8.GetString(buffer, 0, result.Count);
                            stringResult.Append(str);
                        }

                    } while (!result.EndOfMessage);

                    OnWebsocketMessageReceived(stringResult.ToString());
                }
            }
            catch (Exception)
            {
                OnError();
            }
        }

        private void OnWebsocketMessageReceived(string jsonText)
        {
            try
            {
                // Deserialize message base class to determine message type.
                MessageBase request = JsonConvert.DeserializeObject<MessageBase>(jsonText);
                if (request == null)
                {
                    Trace.WriteLine("OnWebsocketMessageReceived: Unable to deserialize base message");
                    return;
                }

                Trace.WriteLine("OnWebsocketMessageReceived: Message type: " + request.MessageType);

                switch (request.MessageType)
                {
                    case MessageType.AddSubscriberResponse:
                        AddSubscriberResponseMessage addSubscriberResponseMessage = JsonConvert.DeserializeObject<AddSubscriberResponseMessage>(jsonText);
                        Trace.WriteLine("Got an AddSubscriberResponseMessage");
                        subscriptionId = addSubscriberResponseMessage.SubscriptionId;
                        break;

                    default:
                        Trace.WriteLine("Unknown message type");
                        break;
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        private void OnClose()
        {
            Trace.WriteLine("OnClose");
        }

        private void OnError()
        {
            Trace.WriteLine("OnError");
        }
    }
}
