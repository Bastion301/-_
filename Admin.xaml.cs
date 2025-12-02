using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MDK
{
    public partial class Admin : Window
    {
        private MDKEntities db = new MDKEntities();

        public class OrderDisplayModel
        {
            public int Id { get; set; }
            public string PartnerName { get; set; }
            public DateTime? CreatedDate { get; set; }
            public string Status { get; set; }
            public decimal TotalCost { get; set; }
        }

        public Admin()
        {
            InitializeComponent();
            LoadAllData();
        }

        private void LoadAllData()
        {
            try
            {
                LoadUsersList();
                LoadPartnersList();
                LoadRecentOrders();
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка загрузки данных: {ex.Message}");
            }
        }

       
        private void LoadUsersList()
        {
            var users = db.users.Include("role").ToList();
            LvUsers.ItemsSource = users;
        }

        private void LoadPartnersList()
        {
            var partners = db.partners_.Include("type_partner_").ToList();
            LvPartners.ItemsSource = partners;
        }

        private void LoadRecentOrders()
        {
            var recentOrders = db.order
                .Include("partners_")
                .Include("status_order")
                .Include("structure_order")
                .OrderByDescending(o => o.created_date)
                .Take(10)
                .ToList()
                .Select(o => new OrderDisplayModel
                {
                    Id = o.id,
                    PartnerName = o.partners_?.name,
                    CreatedDate = o.created_date,
                    Status = o.status_order?.name,
                    TotalCost = o.structure_order.Sum(item =>
                        Convert.ToDecimal(item.agreed_price ?? 0) * (item.quantity ?? 0))
                })
                .ToList();

            LvRecentOrders.ItemsSource = recentOrders;
        }

        // Обработчики событий кнопок
        private void BtnEditUser_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is users user)
            {
                OpenUserEditor(user.id);
            }
        }

        private void BtnEditPartner_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is partners_ partner)
            {
                OpenPartnerEditor(partner.id);
            }
        }

        private void BtnViewOrder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is OrderDisplayModel order)
            {
                OpenOrderDetails(order.Id);
            }
        }

        private void OpenUserEditor(int userId)
        {
            var editor = new editUser(userId, false);
            if (editor.ShowDialog() == true)
            {
                LoadUsersList();
            }
        }

        private void OpenPartnerEditor(int partnerId)
        {
            // Используем окно редактирования партнера
            var editor = new editUser(partnerId, "partner", false);
            if (editor.ShowDialog() == true)
            {
                LoadPartnersList();
            }
        }

        private void OpenOrderDetails(int orderId)
        {
            var orderWindow = new Order(orderId);
            orderWindow.ShowDialog();
        }

        private void BtnAddUser_Click(object sender, RoutedEventArgs e)
        {
            var editor = new editUser(0, true);
            if (editor.ShowDialog() == true)
            {
                LoadUsersList();
            }
        }

        private void BtnAddPartner_Click(object sender, RoutedEventArgs e)
        {
            var editor = new editUser(0, "partner", true);
            if (editor.ShowDialog() == true)
            {
                LoadPartnersList();
            }
        }

        private void BtnRefreshUsers_Click(object sender, RoutedEventArgs e)
        {
            LoadUsersList();
        }

        private void BtnRefreshPartners_Click(object sender, RoutedEventArgs e)
        {
            LoadPartnersList();
        }

        private void BtnRefreshOrders_Click(object sender, RoutedEventArgs e)
        {
            LoadRecentOrders();
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            if (ConfirmExit("Вы уверены, что хотите выйти?"))
            {
                ReturnToLogin();
            }
        }

        private bool ConfirmExit(string message)
        {
            return MessageBox.Show(message, "Подтверждение выхода",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
        }

        private void ReturnToLogin()
        {
            var loginWindow = new MainWindow();
            loginWindow.Show();
            this.Close();
        }

        private void ShowError(string message)
        {
            MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}