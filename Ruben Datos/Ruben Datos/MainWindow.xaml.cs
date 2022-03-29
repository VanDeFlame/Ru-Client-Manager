using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.IO;
using System.Windows.Controls;
using System.Windows.Input;
using DocumentFormat.OpenXml.Spreadsheet;
using SpreadsheetLight;

namespace Ruben_Datos
{
    public partial class MainWindow : Window
    {
        //List<Client> Clients = new List<Client>();
        List<Client> ClientsNice = new List<Client>();
        List<Client> ClientsExpired = new List<Client>();
        List<Client> ClientsExpiring = new List<Client>();
        public const string DateTimeUiFormat = "dd/MM/yy";
        string filter = "Name";
        
        public MainWindow()
        {
            InitializeComponent();
            inputDate.SelectedDate = DateTime.Today;
            ReadExcel();
        }

        private void Create_Client(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(inputName.Text)
                || string.IsNullOrEmpty(inputDays.Text)
                || string.IsNullOrEmpty(inputPlatform.Text)
                || inputName.Text == "Nombre del Cliente"
                || inputDays.Text == "Días Contratados"
                || inputPlatform.Text == "Platform")
            {
                return;
            }

            string clientName = inputName.Text;
            string clientPlatform = inputPlatform.Text;
            int clientDays = Convert.ToInt16(inputDays.Text);
            DateTime thisDay = Convert.ToDateTime(inputDate.Text);
            TimeSpan difference = thisDay.AddDays(clientDays) - DateTime.Now;
            //Console.WriteLine(thisDay);
            Client newClient = new Client()
            {
                Name = clientName,
                Days = clientDays,
                DateEmited = thisDay,
                DateExpired = thisDay.AddDays(clientDays),
                Platform = clientPlatform,
                DaysLeft = (int)difference.TotalDays
            };

            //Clients.Add(newClient);

            if (newClient.DaysLeft - 7 > 0)
            {
                ClientsNice.Add(newClient);
                listClients.Items.Refresh();
            }
            else if (newClient.DaysLeft < 0)
            {
                ClientsExpired.Add(newClient);
                listExpired.Items.Refresh();
            }
            else
            {
                ClientsExpiring.Add(newClient);
                listExpiring.Items.Refresh();
            }
        }
        private void ReadExcel()
        {
            string pathFile = AppDomain.CurrentDomain.BaseDirectory + "datos.xlsx";
            SLDocument sL = new SLDocument(pathFile);
            int iRow = 4;

            while(!string.IsNullOrEmpty(sL.GetCellValueAsString(iRow, 1)))
            {                
                string clientName = sL.GetCellValueAsString(iRow, 1);
                DateTime clientEmited = sL.GetCellValueAsDateTime(iRow, 2);
                int clientDays = sL.GetCellValueAsInt32(iRow, 3);
                DateTime clientExpired = clientEmited.AddDays(clientDays);
                string clientPlatform = sL.GetCellValueAsString(iRow, 7);
                TimeSpan difference = clientExpired - DateTime.Today;

                Client newClient = new Client() {
                    Name = clientName,
                    Days = clientDays,
                    DateEmited = clientEmited,
                    DateExpired = clientExpired,
                    Platform = clientPlatform,
                    DaysLeft = (int)difference.TotalDays
                };

                if (newClient.DaysLeft - 7 > 0) ClientsNice.Add(newClient);
                else if (newClient.DaysLeft < 0) ClientsExpired.Add(newClient);
                else ClientsExpiring.Add(newClient);

                iRow++;
            }

            listClients.ItemsSource = ClientsNice;
            listExpired.ItemsSource = ClientsExpired;
            listExpiring.ItemsSource = ClientsExpiring;
        }

        private void Input_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox inputText = sender as TextBox;

            if (inputText.Text == "Nombre del Cliente" || inputText.Text == "Días Contratados" || inputText.Text == "Plataforma" || inputText.Text == "Buscar")
            {
                inputText.Text = "";
            }
        }

        private void OnlyNumbers(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsValid(((TextBox)sender).Text + e.Text);
        }

        public static bool IsValid(string str)
        {
            int i;
            return int.TryParse(str, out i) && i >= 1 && i <= 9999;
        }
        
        private void Excel(object sender, RoutedEventArgs e)
        {
            UpdateAlerts();
            string pathFile = AppDomain.CurrentDomain.BaseDirectory + "datos.xlsx";

            SLDocument sL = new SLDocument();

            System.Data.DataTable dt = new System.Data.DataTable();
            System.Data.DataTable dtoday = new System.Data.DataTable();

            //Colums
            dtoday.Columns.Add("Hoy", typeof(DateTime));
            dt.Columns.Add("Nombre", typeof(string));
            dt.Columns.Add("Emisión", typeof(DateTime));
            dt.Columns.Add("Días", typeof(int));
            dt.Columns.Add("Expiración", typeof(DateTime));
            dt.Columns.Add("Alertas", typeof(string));
            dt.Columns.Add("Vence en", typeof(int));
            dt.Columns.Add("Plataforma", typeof(string));

            //Rows
            dtoday.Rows.Add(DateTime.Today);

            List<Client> AllLists = new List<Client>();
            AllLists.AddRange(ClientsNice);
            AllLists.AddRange(ClientsExpired);
            AllLists.AddRange(ClientsExpiring);
            foreach (Client i in AllLists)
            {
                int ind = AllLists.IndexOf(i) + 4;
                string alert = $"=IFS(F{ind}>7;\"Falta\"; F{ind}>=0; \"A vencer\"; F{ind}<0; \"Vencido\")";
                dt.Rows.Add(i.Name, i.DateEmited, i.Days, i.DateExpired, alert, i.DaysLeft, i.Platform);
            }

            SLStyle style = sL.CreateStyle();

            //Estilo general
            style.FormatCode = "";
            style.SetFont("Arial", 11);
            style.SetHorizontalAlignment(HorizontalAlignmentValues.Center);
            sL.SetColumnStyle(1, 20, style);

            //Estilo columnas
            style.FormatCode = "dd/mm/yyyy";
            sL.SetColumnStyle(2, style);
            sL.SetColumnStyle(4, style);

            //Estilo headers            
            style.FormatCode = "";
            style.SetFont("Arial", 12);
            style.SetFontBold(true);
            style.SetFontColor(System.Drawing.Color.Blue);
            sL.SetRowStyle(3, style);
            sL.SetCellStyle(1, 2, style);

            style.SetBottomBorder(BorderStyleValues.Medium, System.Drawing.Color.Blue);
            sL.SetCellStyle(3, 1, 3, 7, style);

            //Exportar excel
            sL.ImportDataTable(1, 2, dtoday, true);
            sL.ImportDataTable(3, 1, dt, true);
            sL.Filter("A3", $"G{AllLists.Count+3}");
            sL.AutoFitColumn(1, 7);
            sL.SaveAs(pathFile);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Excel(null, null);
                        
            string msg = "¿Exportaste el excel?";
            MessageBoxResult result =
                MessageBox.Show(
                msg,
                "Data App",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.No)
            {
                e.Cancel = true;
            }
        }

        private void ItemTemplateChange(object sender, SizeChangedEventArgs e)
        {
            Window window = sender as Window;

            if (ItemTemplateTrigger.IsChecked == false && (window.Width > 1200 || window.WindowState == WindowState.Maximized))
            {
                ItemTemplateTrigger.IsChecked = true;
            }
            else if (ItemTemplateTrigger.IsChecked == true && (window.Width <= 1200 && window.WindowState == WindowState.Normal))
            {
                ItemTemplateTrigger.IsChecked = false;
            }
        }

        private void Sort(object sender, RoutedEventArgs e)
        {
            Button filterButton = sender as Button;
            filter = filterButton.Name.Remove(0,1);

            listClients.Items.SortDescriptions.Clear();
            listClients.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription(filter, System.ComponentModel.ListSortDirection.Ascending));

            listExpiring.Items.SortDescriptions.Clear();
            listExpiring.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription(filter, System.ComponentModel.ListSortDirection.Ascending));

            listExpired.Items.SortDescriptions.Clear();
            listExpired.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription(filter, System.ComponentModel.ListSortDirection.Ascending));
        }

        private void UpdateClient(object sender, RoutedEventArgs e)
        {
            TextBox senderInput = sender as TextBox;
            Grid item = senderInput.Parent as Grid;
            TextBox iName = item.Children[0] as TextBox;
            TextBox iPlatform = item.Children[1] as TextBox;
            TextBox iEmited = item.Children[2] as TextBox;
            TextBox iDays = item.Children[3] as TextBox;
            TextBlock iExpired = item.Children[4] as TextBlock;
            TextBlock iLeft = item.Children[5] as TextBlock;
           
            if (iName.Text != null && iPlatform.Text != null && iEmited.Text != null && iDays.Text != null && iExpired.Text != null && iLeft.Text != null)
            {
                string name = iName.Text;
                string platform = iPlatform.Text;
                int day = int.Parse(iDays.Text);
                DateTime emi = DateTime.ParseExact(iEmited.Text, DateTimeUiFormat, null);
                DateTime exp = emi.AddDays(day);

                TimeSpan difference = exp - DateTime.Today;
                Console.WriteLine(difference.TotalDays);

                int actualList = int.Parse(iLeft.Text);

                if (actualList > 7)
                {
                    Client thisClient = ClientsNice.Find(x => x.Name.Contains(name) && x.Platform.Contains(platform) && x.Days.Equals(day));
                    int pos = ClientsNice.IndexOf(thisClient);
                    thisClient.DaysLeft = Convert.ToInt32(difference.TotalDays);
                    thisClient.DateExpired = exp;

                    if(difference.TotalDays <= 7)
                    {
                        ClientsNice.RemoveAt(pos);
                        if (difference.TotalDays < 0) ClientsExpired.Add(thisClient);
                        else ClientsExpiring.Add(thisClient);
                    }
                }
                else if (actualList < 0)
                {
                    Client thisClient = ClientsExpired.Find(x => x.Name.Contains(name) && x.Platform.Contains(platform) && x.Days.Equals(day));
                    int pos = ClientsExpired.IndexOf(thisClient);
                    thisClient.DaysLeft = Convert.ToInt32(difference.TotalDays);
                    thisClient.DateExpired = exp;

                    if (difference.TotalDays >= 0)
                    {
                        ClientsExpired.RemoveAt(pos);
                        if (difference.TotalDays > 7) ClientsNice.Add(thisClient);
                        else ClientsExpiring.Add(thisClient);
                    }
                }
                else if (actualList <= 7 && actualList >= 0)
                {
                    Client thisClient = ClientsExpiring.Find(x => x.Name.Contains(name) && x.Platform.Contains(platform) && x.Days.Equals(day));
                    int pos = ClientsExpiring.IndexOf(thisClient);
                    thisClient.DaysLeft = Convert.ToInt32(difference.TotalDays);
                    thisClient.DateExpired = exp;

                    if(difference.TotalDays < 0 || difference.TotalDays > 7)
                    {
                        ClientsExpiring.RemoveAt(pos);
                        if (difference.TotalDays < 0) ClientsExpired.Add(thisClient);
                        else ClientsNice.Add(thisClient);
                    }
                }
                if (searchBar.Text != "" && searchBar.Text != null) ListSearch(null, null);
                listClients.Items.Refresh();
                listExpired.Items.Refresh();
                listExpiring.Items.Refresh();
            }
        }

        private void ListSearch(object sender, RoutedEventArgs e)
        {
            string filterS = searchBar.Text.ToLower();

            if(filterS != "" && filterS != null)
            {
                listClients.ItemsSource = ClientsNice.FindAll(x => x.Name.ToLower().Contains(filterS));
                listExpired.ItemsSource = ClientsExpired.FindAll(x => x.Name.ToLower().Contains(filterS));
                listExpiring.ItemsSource = ClientsExpiring.FindAll(x => x.Name.ToLower().Contains(filterS));
            }
            else
            {
                listClients.ItemsSource = ClientsNice;
                listExpired.ItemsSource = ClientsExpired;
                listExpiring.ItemsSource = ClientsExpiring;
            }
        }

        private void Config(object sender, RoutedEventArgs e)
        {
            string pathFile = AppDomain.CurrentDomain.BaseDirectory + "alertOptions.txt";

            MessageBoxResult result =
                MessageBox.Show(
                "¿Activar las alertas?",
                "Config alertas",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.No)
            {
                string[] options = { "Alerts: False" };
                File.WriteAllLines(pathFile, options);
            }
            else
            {
                string[] options = { "Alerts: True" };
                File.WriteAllLines(pathFile, options);
                UpdateAlerts();
            }
        }

        private void UpdateAlerts()
        {
            string pathFile = AppDomain.CurrentDomain.BaseDirectory + "alertOptions.txt";
            string[] options = { "Alerts: ", "Startup: False", "Por vencer: ", "Vencido: " };
            options[0] = File.ReadLines(pathFile).First();
            options[2] = "Por vencer: " + ClientsExpiring.Count();
            options[3] = "Vencido: " + ClientsExpired.Count();
            File.WriteAllLines(pathFile, options);
        }
    }

    public class Client
    {
        public string Name { get; set; }
        public int Days { get; set; }
        public DateTime DateEmited { get; set; }
        public DateTime DateExpired { get; set; }
        public string Platform { get; set; }
        public int DaysLeft { get; set; }
    }
}