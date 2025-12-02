using System;
using System.Linq;
using System.Windows;

namespace MDK
{
    public partial class editUser : Window
    {
        private MDKEntities db = new MDKEntities();
        private int editId;
        private string editType; // "user" или "partner"
        private bool isNew; // true если создание нового пользователя

        public editUser(int id, bool isNew = false) : this(id, "user", isNew)
        {
        }

        public editUser(int id, string type, bool isNew = false)
        {
            InitializeComponent();
            this.editId = id;
            this.editType = type;
            this.isNew = isNew;

            SetupForm();
            LoadData();
        }

        private void SetupForm()
        {
            if (editType == "user")
            {
                TbTitle.Text = isNew ? "Добавление пользователя" : "Редактирование пользователя";
                UserPanel.Visibility = Visibility.Visible;
                PartnerPanel.Visibility = Visibility.Collapsed;

                // Загружаем роли
                CbRole.ItemsSource = db.role.ToList();
            }
            else if (editType == "partner")
            {
                TbTitle.Text = isNew ? "Добавление партнера" : "Редактирование партнера";
                UserPanel.Visibility = Visibility.Collapsed;
                PartnerPanel.Visibility = Visibility.Visible;

                // Загружаем типы партнеров
                CbPartnerType.ItemsSource = db.type_partner_.ToList();
            }
        }

        private void LoadData()
        {
            try
            {
                if (isNew) return;

                if (editType == "user")
                {
                    var user = db.users.Include("role").FirstOrDefault(u => u.id == editId);
                    if (user != null)
                    {
                        TbLogin.Text = user.login;
                        TbPassword.Text = user.password;
                        CbRole.SelectedValue = user.id_role;
                    }
                }
                else if (editType == "partner")
                {
                    var partner = db.partners_.Include("type_partner_").FirstOrDefault(p => p.id == editId);
                    if (partner != null)
                    {
                        TbCompanyName.Text = partner.name;
                        TbDirector.Text = partner.director;
                        CbPartnerType.SelectedValue = partner.type_partner;
                        TbEmail.Text = partner.email;
                        TbPhone.Text = partner.phone;
                        TbAddress.Text = partner.adress;
                        TbInn.Text = partner.inn?.ToString();
                        TbRating.Text = partner.reiting?.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Валидация данных
                if (!ValidateData())
                    return;

                if (editType == "user")
                {
                    SaveUser();
                }
                else if (editType == "partner")
                {
                    SavePartner();
                }

                db.SaveChanges();
                MessageBox.Show("Данные успешно сохранены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateData()
        {
            if (editType == "user")
            {
                if (string.IsNullOrWhiteSpace(TbLogin.Text))
                {
                    MessageBox.Show("Введите логин!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TbLogin.Focus();
                    return false;
                }

                if (string.IsNullOrWhiteSpace(TbPassword.Text))
                {
                    MessageBox.Show("Введите пароль!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TbPassword.Focus();
                    return false;
                }

                if (CbRole.SelectedItem == null)
                {
                    MessageBox.Show("Выберите роль!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    CbRole.Focus();
                    return false;
                }
            }
            else if (editType == "partner")
            {
                if (string.IsNullOrWhiteSpace(TbCompanyName.Text))
                {
                    MessageBox.Show("Введите название компании!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TbCompanyName.Focus();
                    return false;
                }

                if (string.IsNullOrWhiteSpace(TbDirector.Text))
                {
                    MessageBox.Show("Введите ФИО директора!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TbDirector.Focus();
                    return false;
                }

                if (CbPartnerType.SelectedItem == null)
                {
                    MessageBox.Show("Выберите тип партнера!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    CbPartnerType.Focus();
                    return false;
                }
            }

            return true;
        }

        private void SaveUser()
        {
            users user;

            if (isNew)
            {
                user = new users();
                db.users.Add(user);
            }
            else
            {
                user = db.users.FirstOrDefault(u => u.id == editId);
            }

            if (user != null)
            {
                user.login = TbLogin.Text;
                user.password = TbPassword.Text;

                if (CbRole.SelectedItem is role selectedRole)
                {
                    user.id_role = selectedRole.id;
                }
            }
        }

        private void SavePartner()
        {
            partners_ partner;

            if (isNew)
            {
                partner = new partners_();
                db.partners_.Add(partner);
            }
            else
            {
                partner = db.partners_.FirstOrDefault(p => p.id == editId);
            }

            if (partner != null)
            {
                partner.name = TbCompanyName.Text;
                partner.director = TbDirector.Text;
                partner.email = TbEmail.Text;
                partner.phone = TbPhone.Text;
                partner.adress = TbAddress.Text;

                // ИНН
                if (double.TryParse(TbInn.Text, out double inn))
                {
                    partner.inn = inn;
                }

                // Рейтинг
                if (double.TryParse(TbRating.Text, out double rating))
                {
                    partner.reiting = rating;
                }

                // Тип партнера
                if (CbPartnerType.SelectedItem is type_partner_ selectedType)
                {
                    partner.type_partner = selectedType.id;
                }
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}