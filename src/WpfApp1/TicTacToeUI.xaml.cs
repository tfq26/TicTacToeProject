using Azure.Storage;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DispatcherTimer checkMsgTimer = new DispatcherTimer();
        private QueueClient m_queueClient1;
        private QueueClient m_queueClient2;
        private QueueClient m_starterQueueClient;
        private int index = 0;
        private char[] boardArr = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i'};
        private char userToken;
        private int playerNumber = 1;
        private QueueClient sendingQueue;
        private QueueClient receivingQueue;

        public MainWindow()
        {
            InitializeComponent();
            InitializeGame();
        }

        private void InitializeGame()
        {
            InitializeQueue();

            checkFirstMessage();

            checkMsgTimer.Interval = TimeSpan.FromMilliseconds(50);
            checkMsgTimer.Tick += CheckMsgTimerOnTick;
            checkMsgTimer.Start();
        }

        private void CheckMsgTimerOnTick(object? sender, EventArgs e)
        {
            canMakeMove();
            CheckWin();
        }

        public void InitializeQueue()
        {
            // Use your Azure Storage account credentials here
            var creds = new StorageSharedKeyCredential("############", "*****************************************************************************==");

            m_queueClient1 = new QueueClient(new Uri("https://mystorageacc2608.queue.core.windows.net/player1"), creds);
            m_queueClient1.CreateIfNotExists();
            m_queueClient2 = new QueueClient(new Uri("https://mystorageacc2608.queue.core.windows.net/player2"), creds);
            m_queueClient2.CreateIfNotExists();
            m_starterQueueClient = new QueueClient(new Uri("https://mystorageacc2608.queue.core.windows.net/token"), creds);
            m_starterQueueClient.CreateIfNotExists();

            m_queueClient1.ClearMessages();
            m_queueClient2.ClearMessages();
        }

        private void UpdateBoard(string btnName, char token)
        {
            String[] testStr = btnName.Split("btn"); //Number of Button pressed to match with button array
            int numPressed = int.Parse(testStr[1]);
            boardArr[numPressed - 1] = token;
        }

        public void SendButtonClicked(object source, RoutedEventArgs args) // Method for button when clicked
        {
            Button tempBtn = source as Button;
            UpdateBoard(tempBtn.Name, userToken);
            tempBtn.Content = userToken;
            tempBtn.Foreground = userToken == 'X' ? Brushes.Blue : Brushes.Red;
            sendMove(tempBtn.Name);
            updateButtonsState(false);
        }

        public void ReceiveButtonClicked(object source, RoutedEventArgs args) // Method for button when clicked
        {

        }

        private void checkFirstMessage()
        {
            Boolean hasFirstMessage = false;
            QueueMessage[] messages = m_starterQueueClient.ReceiveMessages();
            if (messages.Length == 0)
            {
                playerNumber = 1;
                TimerText.Text = "Player 1";
                receivingQueue = m_queueClient2;
                sendingQueue = m_queueClient1;
                //StarterText.Text = "I am starting";
                m_starterQueueClient.SendMessage("game now started for real");
                updateButtonsState(true);
            }
            else
            {
                playerNumber = 2;
                TimerText.Text = "Player 2";
                //StarterText.Text = "I am second so not starting";
                receivingQueue = m_queueClient1;
                sendingQueue = m_queueClient2;
                m_starterQueueClient.ClearMessages();
                updateButtonsState(false);
            }
            setUserToken();
        }

        public void setUserToken()
        {
            if (playerNumber == 1)
            {
                userToken = 'X';
            }
            else
            {
                userToken = 'O';
            }
        }

        private void sendMove(string moveStr)
        {

            if (sendingQueue != null)
            {
                sendingQueue.SendMessage(moveStr);
            }


        }

        private bool DidPlayerWin(char playerChar)
        {
            return ((boardArr[0] == boardArr[1] && boardArr[1] == boardArr[2] && boardArr[2] == playerChar) ||
                   (boardArr[0] == boardArr[3] && boardArr[3] == boardArr[6] && boardArr[6] == playerChar) ||
                   (boardArr[0] == boardArr[4] && boardArr[4] == boardArr[8] && boardArr[8] == playerChar) ||
                   (boardArr[3] == boardArr[4] && boardArr[4] == boardArr[5] && boardArr[5] == playerChar) ||
                   (boardArr[6] == boardArr[7] && boardArr[7] == boardArr[8] && boardArr[8] == playerChar) ||
                   (boardArr[1] == boardArr[4] && boardArr[4] == boardArr[7] && boardArr[7] == playerChar) ||
                   (boardArr[2] == boardArr[5] && boardArr[5] == boardArr[8] && boardArr[8] == playerChar) ||
                   (boardArr[2] == boardArr[4] && boardArr[4] == boardArr[6] && boardArr[6] == playerChar));
        }

        private void CheckWin()
        {
            string winningPlayer = "";
            if (DidPlayerWin('X'))
            {
                winningPlayer = "Player 1";
            }
            else
            {
                if (DidPlayerWin('O'))
                {
                    winningPlayer = "Player 2";
                }
            }

            if (winningPlayer != "")
            {
                checkMsgTimer.Stop();
                updateButtonsState(false);
                TimerText.Foreground = Brushes.Green;
                TimerText.Text = $"Game Over - {winningPlayer} has won";

                DispatcherTimer gameOverTimer = new DispatcherTimer();
                gameOverTimer.Interval = TimeSpan.FromMilliseconds(2000);
                gameOverTimer.Tick += delegate (object? sender, EventArgs e)
                {
                    gameOverTimer.Stop();
                    this.Close();
                };
                gameOverTimer.Start();
            }
            
        }

        private void canMakeMove()
        {
            if (receivingQueue != null)
            {
                QueueMessage[] messages = receivingQueue.ReceiveMessages();
                if (messages.Length > 0)
                {
                    var lastMessage = messages.LastOrDefault();

                    if (lastMessage != null)
                    {
                        updateButtonsState(true, lastMessage.MessageText);
                        UpdateBoard(lastMessage.MessageText, userToken == 'X' ? 'O' : 'X');
                        receivingQueue.ClearMessages();
                    }
                }
            }
        }

        private void updateButtonsState(bool enabled, string buttonNumber = "")
        {
            foreach (Control ctrl in grid.Children)
            {
                if (ctrl is Button)
                {
                    Button btnCtrl = ctrl as Button;

                    btnCtrl.IsEnabled = enabled;

                    if (btnCtrl.Name == buttonNumber)
                    {
                        btnCtrl.Content = userToken == 'X' ? "O" : "X";
                        btnCtrl.Foreground = btnCtrl.Content.ToString() == "X" ? Brushes.Blue : Brushes.Red;
                    }
                }
            }
        }

    }
}
