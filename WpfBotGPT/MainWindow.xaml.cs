using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using System.Threading;
using System.Threading;
using OpenQA.Selenium.BiDi.Modules.Input;


namespace WpfBotGPT
{
    public partial class MainWindow : Window


    {
        private TelegramBotClient botClient;
        private Timer timer;
        private long chatId;

        public MainWindow()
        {
            InitializeComponent();
        }

        //что деалать при нажатии кнопки 
        private async void OnStartButtonClick(object sender, RoutedEventArgs e)
        {


            string botToken = BotTokenTextBox.Text;
            string login = LoginTextBox.Text;
            string password = PasswordBox.Password;





            if (string.IsNullOrEmpty(botToken) || string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                UpdateStatus("Пожалуйста, заполните все поля.", System.Windows.Media.Brushes.Red);

                return;
            }


            try
            {   //содание экзепляра бота
                botClient = new TelegramBotClient(botToken);

            }
            catch (Exception)
            {

                StatusTextBlock.Text = $"Ошибка неверный токен:\n";
                StatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
            }




            // Получение chatId, для этого нужно послать в бот любое сообщение
            chatId = await GetChatIdAsync();
            if (chatId == 0)
            {
                StatusTextBlock.Text = "Не удалось получить chatId. Убедитесь, что бот получил сообщение \n (отправте любое сообщение в бот) .";
                StatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                return;
            }


            //запуск таимера на посторный запуск
            timer = new Timer(async _ => await SendPeriodicMessageAsync(login, password), null, TimeSpan.Zero, TimeSpan.FromMinutes(4));





        }
        // метод, для Получение chatId
        private async Task<long> GetChatIdAsync()
        {
            try
            {
                var updates = await botClient.GetUpdatesAsync();
                var lastUpdate = updates.LastOrDefault();

                if (lastUpdate != null && lastUpdate.Message != null && lastUpdate.Message.Chat != null)
                {
                    return lastUpdate.Message.Chat.Id;
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Ошибка получения chatId или botToken, \n Напишите любое сообщение в бот  : {ex.Message}";
                StatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
            }

            return 0;
        }
        // запуск парсера и отправка сообщения
        private async Task SendPeriodicMessageAsync(string login, string password)
        {
            string extractedText = await ExtractTextFromWebsiteAsync(login, password);

            if (!string.IsNullOrEmpty(extractedText))
            {
                await SendMessageToTelegramAsync(chatId, extractedText);
            }
        }

        // парсер
        private async Task<string> ExtractTextFromWebsiteAsync(string login, string password)
        {
            string extractedText = string.Empty;

            // Настройка Selenium WebDriver
            var options = new ChromeOptions();
            options.AddArgument("--headless"); // Запуск в фоновом режиме

            var relativePathToCD = @"bin\chromedriver134.exe";
            //var relativePathToCD = "D:\\Users\\jon\\Desktop\\java\\chromedriver134.exe";

            using (var driver = new ChromeDriver(relativePathToCD))

            {
                try
                {
                    driver.Navigate().GoToUrl("https://otrs.efsol.ru/otrs/index.pl?Action=AgentDashboard"); // Замените на URL сайта

                    //окно разворачивается на полный экран
                    driver.Manage().Window.Maximize();
                    await Task.Delay(3000);


                    // Ввод логина и пароля
                    driver.FindElement(By.XPath("//*[@id=\"User\"]")).SendKeys(login);

                    //вод пароля
                    driver.FindElement(By.XPath("//*[@id=\"Password\"]")).SendKeys(password);

                    //воити клик
                    driver.FindElement(By.XPath("//*[@id=\"LoginButton\"]")).Click();


                    // Ожидание загрузки страницы
                    await Task.Delay(3000);


                    // Первый XPath  на заблокирование заявки
                    string firstXPath = "//*[@id=\"ToolBar\"]/li[3]/a/span";


                    // Попытка найти элемент по первому XPath
                    if (IsElementPresent(driver, firstXPath))
                    {


                        // клик на заблокирование заявки
                        driver.FindElement(By.XPath("//*[@id=\"ToolBar\"]/li[3]/a/span")).Click();


                        await Task.Delay(3000);
                        //перечисление количества заяавок
                        IList<IWebElement> elementListsImg = driver.FindElements(By.XPath("//table[@class='TableSmall NoCellspacing']/tbody/tr //td[@class='UnreadArticles']"));
                        // Console.WriteLine("Количество непрочитаных заявок:  " + elementListsImg.Count);

                        //номер и тема заявки
                        string secondXPath = "//table[@class='TableSmall NoCellspacing']/tbody/tr //td[@class='UnreadArticles']/following-sibling::td[1]";

                        //это обработака нужна если есть зачвки по времени но без коментариев
                        if (IsElementPresent(driver, secondXPath))
                        {
                            //номер и тема заявки
                            IWebElement table1 = driver.FindElement(By.XPath(secondXPath));


                            String numberTiket = table1.Text;
                            //  Console.WriteLine("Номер заявки: " + numberTiket);

                            //тема и тема заявки
                            IWebElement table2 = driver.FindElement(By.XPath("//table[@class='TableSmall NoCellspacing']/tbody/tr //td[@class='UnreadArticles']/following-sibling::td[4]"));
                            String titleTiket = table2.Text;
                            //  Console.WriteLine("Тема заявки: " + titleTiket);


                            // клик на аватар
                            driver.FindElement(By.XPath("//*[@id=\"ToolBar\"]/li[1]/a")).Click();

                            // клик на выход
                            driver.FindElement(By.XPath("//*[@id=\"LogoutButton\"]")).Click();

                            await Task.Delay(4000);

                            String element = " Не прочитаных: " + elementListsImg.Count.ToString() + "\n #️⃣ " + numberTiket + "\n Тема: " + titleTiket;



                            extractedText = element;
                        }
                        else
                        {
                            await Task.Delay(4000);
                            // клик на аватар
                            driver.FindElement(By.XPath("//*[@id=\"ToolBar\"]/li[1]/a")).Click();

                            // клик на выход
                            driver.FindElement(By.XPath("//*[@id=\"LogoutButton\"]")).Click();

                            await Task.Delay(4000);

                            extractedText = null;

                            UpdateStatus("Не прочитанных заявок нет", System.Windows.Media.Brushes.Green);

                        }
                    }
                    else
                    {

                        await Task.Delay(4000);
                        // клик на аватар
                        driver.FindElement(By.XPath("//*[@id=\"ToolBar\"]/li[1]/a")).Click();

                        // клик на выход
                        driver.FindElement(By.XPath("//*[@id=\"LogoutButton\"]")).Click();

                        await Task.Delay(4000);

                        extractedText = null;

                        UpdateStatus("Не прочитанных заявок нет", System.Windows.Media.Brushes.Green);

                    }


                }
                catch (Exception ex)
                {
                    UpdateStatus($"Ошибка: {ex.Message}", System.Windows.Media.Brushes.Red);



                }
            }

            return extractedText;
        }
        //метод отправки сообщения 
        private async Task SendMessageToTelegramAsync(long chatId, string message)
        {
            try
            {

                await botClient.SendTextMessageAsync(chatId, message);
                UpdateStatus("Сообщение отправлено в Telegram.", System.Windows.Media.Brushes.Green);

            }
            catch (Exception ex)
            {
                UpdateStatus($"Ошибка отправки сообщения: {ex.Message}", System.Windows.Media.Brushes.Red);

            }
        }


        // метод использует Dispatcher.Invoke для обновления StatusTextBlock в потоке UI. иначе ошибка выхода из потока
        private void UpdateStatus(string message, System.Windows.Media.Brush color)
        {
            Dispatcher.Invoke(() =>
            {
                StatusTextBlock.Text = message;
                StatusTextBlock.Foreground = color;
            });
        }


        // Метод для проверки наличия элемента
        static bool IsElementPresent(IWebDriver driver, string xPath)
        {
            try
            {
                driver.FindElement(By.XPath(xPath));
                return true;
            }
            catch (NoSuchElementException)
            {
                return false;
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}