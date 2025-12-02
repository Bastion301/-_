using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace MDK
{
    public partial class MainWindow : Window
    {
        private MDKEntities db = new MDKEntities();

        public MainWindow()
        {
            InitializeComponent();
            SetupEventHandlers();
        }

        private void SetupEventHandlers()
        {
            Loaded += OnWindowLoaded;
            TB_Login.KeyDown += OnLoginKeyDown;
            TB_Password.KeyDown += OnPasswordKeyDown;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            TB_Login.Focus();
        }

        private void OnLoginKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                TB_Password.Focus();
        }

        private void OnPasswordKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                PerformLogin();
        }

        private void BTN_Auth_Click(object sender, RoutedEventArgs e)
        {
            PerformLogin();
        }

        private void PerformLogin()
        {
            string login = TB_Login.Text.Trim();
            string password = TB_Password.Password;

            if (!ValidateLoginInput(login, password))
                return;

            try
            {
                AuthenticateUser(login, password);
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Ошибка подключения к базе данных: {ex.Message}");
            }
        }

        private bool ValidateLoginInput(string login, string password)
        {
            if (string.IsNullOrWhiteSpace(login))
            {
                ShowErrorMessage("Введите логин");
                TB_Login.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ShowErrorMessage("Введите пароль");
                TB_Password.Focus();
                return false;
            }

            return true;
        }

        private void AuthenticateUser(string login, string password)
        {
            var user = db.users.FirstOrDefault(u => u.login == login && u.password == password);

            if (user != null)
            {
                OpenUserSession(user);
            }
            else
            {
                ShowErrorMessage("Неверный логин или пароль.");
                TB_Password.Password = "";
                TB_Password.Focus();
            }
        }

        private void OpenUserSession(users user)
        {
            switch (user.id_role)
            {
                case 1: // Админ
                    OpenAdminPanel();
                    break;

                case 2: // Менеджер
                    OpenManagerPanel();
                    break;

                case 3: // Партнёр
                    OpenPartnerPanel(user);
                    break;

                default:
                    ShowErrorMessage("Неизвестная роль пользователя.");
                    break;
            }
        }

        private void OpenAdminPanel()
        {
            Admin adminWindow = new Admin();
            adminWindow.Show();
            this.Close();
        }

        private void OpenManagerPanel()
        {
            Manager managerWindow = new Manager();
            managerWindow.Show();
            this.Close();
        }

        private void OpenPartnerPanel(users user)
        {
            var partner = db.partners_.FirstOrDefault(p => p.name.Contains(user.login) || p.email == user.login);
            int partnerId = partner?.id ?? user.id;

            Partner partnerWindow = new Partner(partnerId);
            partnerWindow.Show();
            this.Close();
        }

        private void ShowErrorMessage(string message)
        {
            MessageBox.Show(message, "Ошибка авторизации", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}