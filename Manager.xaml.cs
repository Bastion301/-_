using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Data.Entity;

namespace MDK
{
    public partial class Manager : Window
    {
        private MDKEntities db = new MDKEntities();

        public class OrderCardViewModel
        {
            public int OrderId { get; set; }
            public string PartnerType { get; set; }
            public string PartnerName { get; set; }
            public string Address { get; set; }
            public string Phone { get; set; }
            public double? Rating { get; set; }
            public decimal TotalCost { get; set; }
            public DateTime? CreatedDate { get; set; }
            public string Status { get; set; }
            public int PartnerId { get; set; }
        }

        public Manager()
        {
            InitializeComponent();
            LoadAllOrders();
        }

        private void LoadAllOrders()
        {
            try
            {
                StackPanelRequests.Children.Clear();

                var orders = db.order
                    .Include("partners_")
                    .Include("partners_.type_partner_")
                    .Include("status_order")
                    .Include("structure_order")
                    .OrderByDescending(o => o.created_date)
                    .ToList();

                foreach (var order in orders)
                {
                    var orderCard = CreateOrderCard(order);
                    StackPanelRequests.Children.Add(orderCard);
                }

                if (!orders.Any())
                {
                    DisplayNoOrdersMessage();
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка загрузки заказов: {ex.Message}");
            }
        }

        private UIElement CreateOrderCard(order order)
        {
            var orderCard = new Border
            {
                Style = (Style)FindResource("RequestItemBorder")
            };

            orderCard.MouseDown += (sender, e) =>
            {
                if (e.ClickCount == 2)
                {
                    OpenOrderDetails(order.id);
                }
            };

            var orderViewModel = new OrderCardViewModel
            {
                OrderId = order.id,
                PartnerType = order.partners_?.type_partner_?.name,
                PartnerName = order.partners_?.name,
                Address = order.partners_?.adress,
                Phone = order.partners_?.phone,
                Rating = order.partners_?.reiting,
                CreatedDate = order.created_date,
                Status = order.status_order?.name,
                PartnerId = order.partners_?.id ?? 0,
                TotalCost = order.structure_order?.Sum(item =>
                    Convert.ToDecimal(item.agreed_price ?? 0) * (item.quantity ?? 0)) ?? 0
            };

            orderCard.Child = CreateOrderCardContent(orderViewModel);
            return orderCard;
        }

        private UIElement CreateOrderCardContent(OrderCardViewModel order)
        {
            var stackPanel = new StackPanel();

            // Верхняя строка
            var topRow = new Grid();
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

            var partnerInfo = new TextBlock
            {
                Text = $"{order.PartnerType} | {order.PartnerName}",
                Style = (Style)FindResource("BoldText"),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(partnerInfo, 0);

            var costInfo = new TextBlock
            {
                Text = $"Стоимость: {order.TotalCost:N2} ₽",
                Style = (Style)FindResource("BoldText"),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetColumn(costInfo, 1);

            topRow.Children.Add(partnerInfo);
            topRow.Children.Add(costInfo);

            // Информация о партнере
            var addressInfo = new TextBlock
            {
                Text = $"Адрес: {order.Address}",
                Style = (Style)FindResource("NormalText"),
                Margin = new Thickness(0, 5, 0, 0)
            };

            var phoneInfo = new TextBlock
            {
                Text = $"Телефон: {order.Phone}",
                Style = (Style)FindResource("NormalText")
            };

            var ratingInfo = new TextBlock
            {
                Text = $"Рейтинг: {order.Rating}",
                Style = (Style)FindResource("NormalText")
            };

            var statusInfo = new TextBlock
            {
                Text = $"Статус: {order.Status}",
                Style = (Style)FindResource("NormalText"),
                FontWeight = FontWeights.Bold
            };

            // Панель действий
            var actionsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 10, 0, 0)
            };

            // Кнопка просмотра деталей
            var detailsButton = new Button
            {
                Content = "Детали",
                Style = (Style)FindResource("AccentButton"),
                Margin = new Thickness(0, 0, 5, 0),
                Tag = order.OrderId
            };
            detailsButton.Click += DetailsButton_Click;

            
            //ratingButton.Click += RatingButton_Click;

            actionsPanel.Children.Add(detailsButton);

            stackPanel.Children.Add(topRow);
            stackPanel.Children.Add(addressInfo);
            stackPanel.Children.Add(phoneInfo);
            stackPanel.Children.Add(ratingInfo);
            stackPanel.Children.Add(statusInfo);
            stackPanel.Children.Add(actionsPanel);

            return stackPanel;
        }

        private void DetailsButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            int orderId = (int)button.Tag;
            OpenOrderDetails(orderId);
        }

        //private void RatingButton_Click(object sender, RoutedEventArgs e)
        //{
        //    var button = (Button)sender;
        //    int partnerId = (int)button.Tag;
        //    ChangePartnerRating(partnerId);
        //}

        //private void ChangePartnerRating(int partnerId)
        //{
        //    try
        //    {
        //        var partner = db.partners_.FirstOrDefault(p => p.id == partnerId);

        //        if (partner != null)
        //        {
        //            var dialog = new RatingChangeWindow(partner);
        //            if (dialog.ShowDialog() == true)
        //            {
        //                // Обновляем рейтинг партнера
        //                partner.reiting = dialog.NewRating;
        //                db.SaveChanges();

        //                MessageBox.Show("Рейтинг партнера успешно изменен", "Успех",
        //                              MessageBoxButton.OK, MessageBoxImage.Information);

        //                LoadAllOrders(); // Обновляем список
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show($"Ошибка изменения рейтинга: {ex.Message}", "Ошибка",
        //                      MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}

        private void OpenOrderDetails(int orderId)
        {
            try
            {
                Order orderWindow = new Order(orderId);
                orderWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия заказа: {ex.Message}");
            }
        }

        private void DisplayNoOrdersMessage()
        {
            var noOrdersText = new TextBlock
            {
                Text = "Заявки не найдены",
                Style = (Style)FindResource("HeaderText"),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 0)
            };
            StackPanelRequests.Children.Add(noOrdersText);
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы уверены, что хотите выйти?", "Подтверждение выхода",
                                        MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                MainWindow loginWindow = new MainWindow();
                loginWindow.Show();
                this.Close();
            }
        }

        private void ShowError(string message)
        {
            MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}