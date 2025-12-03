using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using WPFLocalizeExtension.Engine;

namespace StoreManager
{
    public partial class CustomMessageBox : Window
    {
        public enum MessageBoxType
        {
            Info,
            Success,
            Warning,
            Error,
            Question
        }

        public enum MessageBoxButtons
        {
            OK,
            OKCancel,
            YesNo,
            YesNoCancel
        }

        public MessageBoxResult Result { get; private set; }

        private CustomMessageBox(string message, string title, MessageBoxType type, MessageBoxButtons buttons)
        {
            InitializeComponent();

            TitleText.Text = title;
            MessageText.Text = message;

            SetupIcon(type);
            SetupButtons(buttons);
        }

        private void SetupIcon(MessageBoxType type)
        {
            Color color;
            string iconPath;

            switch (type)
            {
                case MessageBoxType.Success:
                    color = Color.FromRgb(16, 185, 129); // #10B981
                    iconPath = "M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41L9 16.17z";
                    break;
                case MessageBoxType.Warning:
                    color = Color.FromRgb(245, 158, 11); // #F59E0B
                    iconPath = "M1 21h22L12 2 1 21zm12-3h-2v-2h2v2zm0-4h-2v-4h2v4z";
                    break;
                case MessageBoxType.Error:
                    color = Color.FromRgb(239, 68, 68); // #EF4444
                    iconPath = "M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12 19 6.41z";
                    break;
                case MessageBoxType.Question:
                    color = Color.FromRgb(59, 130, 246); // #3B82F6
                    iconPath = "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 17h-2v-2h2v2zm2.07-7.75l-.9.92C13.45 12.9 13 13.5 13 15h-2v-.5c0-1.1.45-2.1 1.17-2.83l1.24-1.26c.37-.36.59-.86.59-1.41 0-1.1-.9-2-2-2s-2 .9-2 2H8c0-2.21 1.79-4 4-4s4 1.79 4 4c0 .88-.36 1.68-.93 2.25z";
                    break;
                default: // Info
                    color = Color.FromRgb(37, 99, 235); // #2563EB
                    iconPath = "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 15h-2v-6h2v6zm0-8h-2V7h2v2z";
                    break;
            }

            HeaderBorder.Background = new SolidColorBrush(color);

            IconCanvas.Children.Clear();
            var path = new Path
            {
                Fill = Brushes.White,
                Data = Geometry.Parse(iconPath),
                Stretch = Stretch.Uniform
            };
            IconCanvas.Children.Add(path);
        }

        private void SetupButtons(MessageBoxButtons buttons)
        {
            ButtonPanel.Children.Clear();

            switch (buttons)
            {
                case MessageBoxButtons.OK:
                    AddButton(GetLocalizedString("MessageBox_buttonOK"), MessageBoxResult.OK, true);
                    break;

                case MessageBoxButtons.OKCancel:
                    AddButton(GetLocalizedString("MessageBox_buttonCancel"), MessageBoxResult.Cancel, false);
                    AddButton(GetLocalizedString("MessageBox_buttonOK"), MessageBoxResult.OK, true);
                    break;

                case MessageBoxButtons.YesNo:
                    AddButton(GetLocalizedString("MessageBox_buttonNo"), MessageBoxResult.No, false);
                    AddButton(GetLocalizedString("MessageBox_buttonYes"), MessageBoxResult.Yes, true);
                    break;

                case MessageBoxButtons.YesNoCancel:
                    AddButton(GetLocalizedString("MessageBox_buttonCancel"), MessageBoxResult.Cancel, false);
                    AddButton(GetLocalizedString("MessageBox_buttonNo"), MessageBoxResult.No, false);
                    AddButton(GetLocalizedString("MessageBox_buttonYes"), MessageBoxResult.Yes, true);
                    break;
            }
        }

        private string GetLocalizedString(string key)
        {
            return LocalizeDictionary.Instance.GetLocalizedObject(
                "StoreManager",
                "Resources.Strings",
                key,
                LocalizeDictionary.Instance.Culture)?.ToString() ?? key;
        }

        private void AddButton(string content, MessageBoxResult result, bool isPrimary)
        {
            var button = new Button
            {
                Content = content,
                Style = isPrimary ? (Style)FindResource("PrimaryButton") : (Style)FindResource("SecondaryButton"),
                Margin = new Thickness(10, 0, 0, 0)
            };

            button.Click += (s, e) =>
            {
                Result = result;
                DialogResult = true;
                Close();
            };

            ButtonPanel.Children.Add(button);
        }

        // Static Show methods
        public static MessageBoxResult Show(string message, string title = "Message", 
            MessageBoxType type = MessageBoxType.Info, MessageBoxButtons buttons = MessageBoxButtons.OK)
        {
            var messageBox = new CustomMessageBox(message, title, type, buttons);
            messageBox.ShowDialog();
            return messageBox.Result;
        }

        public static MessageBoxResult ShowInfo(string message, string title = "Information")
        {
            return Show(message, title, MessageBoxType.Info, MessageBoxButtons.OK);
        }

        public static MessageBoxResult ShowSuccess(string message, string title = "Success")
        {
            return Show(message, title, MessageBoxType.Success, MessageBoxButtons.OK);
        }

        public static MessageBoxResult ShowWarning(string message, string title = "Warning")
        {
            return Show(message, title, MessageBoxType.Warning, MessageBoxButtons.OK);
        }

        public static MessageBoxResult ShowError(string message, string title = "Error")
        {
            return Show(message, title, MessageBoxType.Error, MessageBoxButtons.OK);
        }

        public static MessageBoxResult ShowQuestion(string message, string title = "Question", 
            MessageBoxButtons buttons = MessageBoxButtons.YesNo)
        {
            return Show(message, title, MessageBoxType.Question, buttons);
        }
    }
}
