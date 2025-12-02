using System;
using System.Linq;
using System.Windows;

namespace MDK
{
    public partial class Order : Window
    {
        private MDKEntities db = new MDKEntities();
        private int orderId;

        public class OrderItemViewModel
        {
            public string ProductName { get; set; }
            public int Quantity { get; set; }
            public decimal Price { get; set; }
            public decimal Total => Price * Quantity;
        }

        public Order(int orderId)
        {
            InitializeComponent();
            this.orderId = orderId;
            LoadOrderData();
        }

        // Конструктор по умолчанию для Designer
        public Order()
        {
            InitializeComponent();
        }

        private void LoadOrderData()
        {
            try
            {
                var order = db.order
                    .Include("partners_")
                    .Include("partners_.type_partner_")
                    .Include("status_order")
                    .Include("structure_order")
                    .FirstOrDefault(o => o.id == orderId);

                if (order != null)
                {
                    TbOrderTitle.Text = $"Состав заказа №{order.id}";
                    TbOrderInfo.Text = $"{order.partners_?.type_partner_?.name} | {order.partners_?.name}";
                    TbOrderDate.Text = $"Дата создания: {order.created_date:dd.MM.yyyy HH:mm}";
                    TbOrderStatus.Text = $"Статус: {order.status_order?.name}";

                    // Загружаем состав заказа
                    var orderItems = order.structure_order.Select(item => new OrderItemViewModel
                    {
                        ProductName = GetProductName(item.id_product),
                        Quantity = item.quantity ?? 0,
                        Price = Convert.ToDecimal(item.agreed_price ?? 0)
                    }).ToList();

                    LvOrderItems.ItemsSource = orderItems;

                    decimal totalCost = orderItems.Sum(item => item.Total);
                    TbTotalCost.Text = $"Общая стоимость: {totalCost:N2} ₽";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных заказа: {ex.Message}");
            }
        }

        private string GetProductName(int? productId)
        {
            if (productId.HasValue)
            {
                var product = db.products_.FirstOrDefault(p => p.id == productId.Value);
                return product?.name ?? "Неизвестный продукт";
            }
            return "Неизвестный продукт";
        }
    }
}