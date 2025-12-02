using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MDK
{
    public partial class Partner : Window
    {
        private MDKEntities db = new MDKEntities();
        private int currentPartnerId;
        private List<RequestItem> currentRequest = new List<RequestItem>();

        public class RequestItem
        {
            public int ProductId { get; set; }
            public string ProductName { get; set; }
            public int Quantity { get; set; }
            public decimal Price { get; set; }
            public decimal Total => Price * Quantity;
        }

        public class OrderViewModel
        {
            public int Id { get; set; }
            public DateTime? CreatedDate { get; set; }
            public string Status { get; set; }
            public int StatusId { get; set; }
            public decimal TotalCost { get; set; }
        }

        public Partner(int userID)
        {
            InitializeComponent();
            currentPartnerId = GetPartnerIdFromUserId(userID);
            LoadPartnerData();
            LoadProductTypes();
            LoadProducts();
            LoadMyOrders();
            UpdateTotalCost();
        }

        private int GetPartnerIdFromUserId(int userId)
        {
            try
            {
                var partner = db.partners_.FirstOrDefault(p => p.id == userId || p.name == db.users.First(u => u.id == userId).login);
                return partner?.id ?? userId;
            }
            catch (Exception)
            {
                return userId;
            }
        }

        private void LoadPartnerData()
        {
            try
            {
                var partner = db.partners_.FirstOrDefault(p => p.id == currentPartnerId);

                if (partner != null)
                {
                    TbPartnerInfo.Text = $"{partner.type_partner_?.name} | {partner.name}";
                    TbPartnerContact.Text = $"Директор: {partner.director} | Телефон: {partner.phone} | Email: {partner.email}";
                    TbPartnerRating.Text = $"Юридический адрес: {partner.adress} | Рейтинг: {partner.reiting}";
                    this.Title = $"Панель партнера - {partner.name}";
                }
                else
                {
                    TbPartnerInfo.Text = "Партнер";
                    TbPartnerContact.Text = "Информация о партнере не найдена";
                    TbPartnerRating.Text = "Обратитесь к менеджеру для настройки профиля";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных партнера: {ex.Message}");
            }
        }

        private void LoadProductTypes()
        {
            try
            {
                CbProductTypes.Items.Clear();
                CbProductTypes.Items.Add(new ComboBoxItem { Content = "Все типы продукции", IsSelected = true });

                var productTypes = db.type_product_.OrderBy(t => t.name).ToList();

                foreach (var type in productTypes)
                {
                    CbProductTypes.Items.Add(new ComboBoxItem
                    {
                        Content = type.name,
                        Tag = type.id
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки типов продукции: {ex.Message}");
            }
        }

        private void LoadProducts(int? typeId = null)
        {
            try
            {
                // Проверяем, что LvProducts существует
                if (LvProducts == null)
                {
                    MessageBox.Show("Ошибка: элемент LvProducts не найден в интерфейсе");
                    return;
                }

                IQueryable<products_> query = db.products_;

                if (typeId.HasValue && typeId.Value > 0)
                {
                    query = query.Where(p => p.id_type_product == typeId.Value);
                }

                var products = query.Include("type_product_").OrderBy(p => p.name).ToList();
                var productViewModels = products.Select(p => new ProductViewModel(p)).ToList();

                LvProducts.ItemsSource = productViewModels;

                if (!productViewModels.Any())
                {
                    MessageBox.Show("Продукция не найдена");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки продукции: {ex.Message}");
            }
        }

        private void LoadMyOrders()
        {
            try
            {
                var orders = db.order
                    .Where(o => o.id_partner == currentPartnerId)
                    .Include("status_order")
                    .Include("structure_order")
                    .OrderByDescending(o => o.created_date)
                    .ToList();

                var orderViewModels = orders.Select(o => new OrderViewModel
                {
                    Id = o.id,
                    CreatedDate = o.created_date,
                    Status = o.status_order?.name,
                    StatusId = o.id_status_order ?? 1,
                    TotalCost = o.structure_order?.Sum(item =>
                        Convert.ToDecimal(item.agreed_price ?? 0) * (item.quantity ?? 0)) ?? 0
                }).ToList();

                LvMyOrders.ItemsSource = orderViewModels;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки заказов: {ex.Message}");
            }
        }

        private void CbProductTypes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CbProductTypes.SelectedItem is ComboBoxItem selectedItem)
            {
                if (selectedItem.Tag != null)
                {
                    LoadProducts(Convert.ToInt32(selectedItem.Tag));
                }
                else
                {
                    LoadProducts();
                }
            }
        }

        private void LvProducts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LvProducts.SelectedItem is ProductViewModel selectedProductVM)
            {
                var selectedProduct = db.products_.FirstOrDefault(p => p.id == selectedProductVM.Id);

                if (selectedProduct != null)
                {
                    TbSelectedProduct.Text = $"{selectedProduct.name} - {selectedProduct.Минимальная_стоимость_для_партнера:N2} ₽";
                    BtnAddToRequest.IsEnabled = true;
                    TbQuantity.Focus();
                    TbQuantity.SelectAll();
                }
            }
            else
            {
                TbSelectedProduct.Text = "Выберите продукт из списка";
                BtnAddToRequest.IsEnabled = false;
            }
        }

        private void BtnAddToRequest_Click(object sender, RoutedEventArgs e)
        {
            if (LvProducts.SelectedItem is ProductViewModel selectedProductVM)
            {
                var selectedProduct = db.products_.FirstOrDefault(p => p.id == selectedProductVM.Id);

                if (selectedProduct != null && int.TryParse(TbQuantity.Text, out int quantity) && quantity > 0)
                {
                    var existingItem = currentRequest.FirstOrDefault(item => item.ProductId == selectedProduct.id);
                    if (existingItem != null)
                    {
                        existingItem.Quantity += quantity;
                    }
                    else
                    {
                        currentRequest.Add(new RequestItem
                        {
                            ProductId = selectedProduct.id,
                            ProductName = selectedProduct.name,
                            Quantity = quantity,
                            Price = Convert.ToDecimal(selectedProduct.Минимальная_стоимость_для_партнера ?? 0)
                        });
                    }

                    UpdateRequestList();
                    UpdateTotalCost();
                    TbQuantity.Text = "1";

                    MessageBox.Show($"Товар '{selectedProduct.name}' добавлен в заявку", "Успешно",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Введите корректное количество (целое число больше 0)", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    TbQuantity.Focus();
                    TbQuantity.SelectAll();
                }
            }
            else
            {
                MessageBox.Show("Выберите товар из списка", "Внимание",
                              MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }

        private void BtnRemoveItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is RequestItem item)
            {
                var result = MessageBox.Show($"Удалить '{item.ProductName}' из заявки?",
                                          "Подтверждение удаления",
                                          MessageBoxButton.YesNo,
                                          MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    currentRequest.Remove(item);
                    UpdateRequestList();
                    UpdateTotalCost();
                }
            }
        }

        private void BtnCancelOrder_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var order = (OrderViewModel)button.DataContext;

            if (order.StatusId == 1)
            {
                var result = MessageBox.Show($"Отменить заказ №{order.Id}?",
                                           "Подтверждение отмены",
                                           MessageBoxButton.YesNo,
                                           MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var dbOrder = db.order.FirstOrDefault(o => o.id == order.Id);
                        if (dbOrder != null)
                        {
                            dbOrder.id_status_order = 7;
                            dbOrder.cancel_reason = "Отменен партнером";
                            db.SaveChanges();

                            MessageBox.Show("Заказ отменен", "Успех",
                                          MessageBoxButton.OK, MessageBoxImage.Information);
                            LoadMyOrders();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка отмены заказа: {ex.Message}");
                    }
                }
            }
            else
            {
                MessageBox.Show("Можно отменять только новые заказы", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnPayPrepayment_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var order = (OrderViewModel)button.DataContext;

            if (order.StatusId == 2)
            {
                var result = MessageBox.Show($"Внести предоплату по заказу №{order.Id}?\nСумма: {order.TotalCost:N2} ₽",
                                           "Подтверждение предоплаты",
                                           MessageBoxButton.YesNo,
                                           MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var dbOrder = db.order.FirstOrDefault(o => o.id == order.Id);
                        if (dbOrder != null)
                        {
                            dbOrder.id_status_order = 3;
                            dbOrder.prepayment_date = DateTime.Now;
                            db.SaveChanges();

                            MessageBox.Show("Предоплата подтверждена! Заказ передан в производство.",
                                          "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                            LoadMyOrders();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка внесения предоплаты: {ex.Message}");
                    }
                }
            }
            else
            {
                MessageBox.Show("Предоплату можно внести только для заказов, ожидающих оплаты",
                              "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnOrderDetails_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var order = (OrderViewModel)button.DataContext;

            try
            {
                Order orderWindow = new Order(order.Id);
                orderWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия деталей заказа: {ex.Message}");
            }
        }

        private void UpdateRequestList()
        {
            LvCurrentRequest.ItemsSource = null;
            LvCurrentRequest.ItemsSource = currentRequest;
            BtnSubmitRequest.IsEnabled = currentRequest.Any();
            BtnClearRequest.IsEnabled = currentRequest.Any();
        }

        private void UpdateTotalCost()
        {
            decimal total = currentRequest.Sum(item => item.Total);
            TbTotalCost.Text = $"Итого: {total:N2} ₽";
        }

        private void BtnSubmitRequest_Click(object sender, RoutedEventArgs e)
        {
            if (!currentRequest.Any())
            {
                MessageBox.Show("Добавьте товары в заявку", "Внимание",
                              MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            try
            {
                var result = MessageBox.Show($"Создать заявку на сумму {currentRequest.Sum(item => item.Total):N2} ₽?\n\nКоличество позиций: {currentRequest.Count}",
                                           "Подтверждение заявки",
                                           MessageBoxButton.YesNo,
                                           MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                var newOrder = new order
                {
                    id_status_order = 2,
                    id_partner = currentPartnerId,
                    id_manager = 2,
                    created_date = DateTime.Now
                };

                db.order.Add(newOrder);
                db.SaveChanges();

                foreach (var item in currentRequest)
                {
                    var structureOrder = new structure_order
                    {
                        id_order = newOrder.id,
                        id_product = item.ProductId,
                        quantity = item.Quantity,
                        agreed_price = Convert.ToDouble(item.Price),
                        id_status_structure_order = 1
                    };
                    db.structure_order.Add(structureOrder);
                }

                db.SaveChanges();

                MessageBox.Show($"Заявка №{newOrder.id} успешно создана!\n\n" +
                              $"Статус: Ожидает предоплаты\n" +
                              $"Внесите предоплату в течение 3 дней\n\n" +
                              $"Общая стоимость: {currentRequest.Sum(item => item.Total):N2} ₽\n" +
                              $"Количество позиций: {currentRequest.Count}",
                              "Заявка создана",
                              MessageBoxButton.OK,
                              MessageBoxImage.Information);

                currentRequest.Clear();
                UpdateRequestList();
                UpdateTotalCost();
                LoadMyOrders();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка создания заявки: {ex.Message}",
                              "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnClearRequest_Click(object sender, RoutedEventArgs e)
        {
            if (currentRequest.Any())
            {
                var result = MessageBox.Show("Очистить всю заявку?",
                                          "Подтверждение",
                                          MessageBoxButton.YesNo,
                                          MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    currentRequest.Clear();
                    UpdateRequestList();
                    UpdateTotalCost();
                }
            }
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

        private void TbQuantity_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                BtnAddToRequest_Click(sender, e);
            }
        }
    }

    public class ProductViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string TypeName { get; set; }
        public decimal MinPartnerPrice { get; set; }
        public double Article { get; set; }

        public ProductViewModel(products_ product)
        {
            Id = product.id;
            Name = product.name;
            TypeName = product.type_product_?.name ?? "Неизвестный тип";
            MinPartnerPrice = Convert.ToDecimal(product.Минимальная_стоимость_для_партнера ?? 0);
            Article = product.Артикул ?? 0;
        }
    }
}