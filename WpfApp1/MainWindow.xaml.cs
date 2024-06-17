using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp1
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // Загрузка данных из БД
            //LoadGame();
            //StartNewGame();
            // Создание игрового поля
            //CreateGameField();
        }
        private string connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=Puzzle;Integrated Security=True";

        // Размер игрового поля
        private const int FIELD_SIZE = 3;

        // Игровое поле
        private int[,] field = new int[FIELD_SIZE, FIELD_SIZE];

        // Координаты пустой клетки
        private int emptyRow, emptyCol;

        // Список фишек
        private List<Button> tiles = new List<Button>();



        private void LoadGame()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Проверка на наличие незавершенной игры
                string query = "SELECT top (1) Field0, Field1, Field2, Field3, Field4, Field5, Field6, Field7, Field8 FROM Games WHERE IsFinished = 0 order by id desc";
                SqlCommand command = new SqlCommand(query, connection);
                SqlDataReader reader = command.ExecuteReader();

                if (reader.Read())
                {
                    // Восстановление незавершенной игры
                    for (int i = 0; i < FIELD_SIZE; i++)
                    {
                        for (int j = 0; j < FIELD_SIZE; j++)
                        {
                            field[i, j] = Convert.ToInt32(reader[i * FIELD_SIZE + j]);
                        }
                    }

                    // Нахождение пустой клетки
                    for (int i = 0; i < FIELD_SIZE; i++)
                    {
                        for (int j = 0; j < FIELD_SIZE; j++)
                        {
                            if (field[i, j] == 0)
                            {
                                emptyRow = i;
                                emptyCol = j;
                                break;
                            }
                        }
                    }
                    CreateGameField();
                }
                else
                {
                    // Новая игра
                    StartNewGame();
                }
            }
        }

        // Создание игрового поля
        private void CreateGameField()
        {
            // Создание кнопок для фишек
            tiles.Clear(); // Очищаем список кнопок перед созданием новых
            for (int i = 0; i < FIELD_SIZE; i++)
            {
                for (int j = 0; j < FIELD_SIZE; j++)
                {
                    Button tile = new Button();
                    tile.Content = field[i, j] != 0 ? field[i, j].ToString() : ""; // Добавляем текст на кнопку
                    tile.FontSize = 30;
                    tile.Click += Tile_Click;

                    // Определение координат пустой клетки
                    if (field[i, j] == 0)
                    {
                        emptyRow = i;
                        emptyCol = j;
                    }

                    // Добавление фишки в список
                    tiles.Add(tile);
                }
            }

            // Размещение фишек на игровом поле
            GameField.Children.Clear();
            for (int i = 0; i < tiles.Count; i++)
            {
                GameField.Children.Add(tiles[i]);
                Grid.SetRow(tiles[i], i / FIELD_SIZE);
                Grid.SetColumn(tiles[i], i % FIELD_SIZE);
            }
        }

        // Обработка клика по фишке
        private void Tile_Click(object sender, RoutedEventArgs e)
        {
            Button tile = (Button)sender;
            int row = Grid.GetRow(tile);
            int col = Grid.GetColumn(tile);

            // Проверка на соседство с пустой клеткой
            if (Math.Abs(row - emptyRow) + Math.Abs(col - emptyCol) == 1)
            {
                // Обмен значениями фишки и пустой клетки
                field[row, col] = 0;
                field[emptyRow, emptyCol] = Convert.ToInt32(tile.Content);

                // Обновление координат пустой клетки
                emptyRow = row;
                emptyCol = col;

                // Сохранение состояния игры
                SaveGame();

                // Перерисовка игрового поля
                CreateGameField();

                // Проверка на победу
                if (IsGameWon())
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();

                        string query = "SELECT StartTime FROM Games WHERE IsFinished = 0";
                        SqlCommand command = new SqlCommand(query, connection);
                        SqlDataReader reader = command.ExecuteReader();

                        if (reader.Read())
                        {
                            DateTime startTime = Convert.ToDateTime(reader["StartTime"]);
                            MessageBox.Show($"Ура! Вы решили головоломку за ! {DateTime.Now - startTime}");
                        }
                        reader.Close();
                        query = "UPDATE Games SET IsFinished = 1 WHERE Id = (SELECT TOP 1 Id FROM Games ORDER BY StartTime DESC)";
                        command = new SqlCommand(query, connection);

                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        // Сохранение состояния игры
        private void SaveGame()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string query = "UPDATE top (1) Games SET ";

                for (int i = 0; i < FIELD_SIZE; i++)
                {
                    for (int j = 0; j < FIELD_SIZE; j++)
                    {
                        query += $"Field{i * FIELD_SIZE + j} = @Field{i * FIELD_SIZE + j}, ";
                    }
                }

                query = query.Substring(0, query.Length - 2);
                query += " WHERE IsFinished = 0 and Id = (SELECT TOP 1 Id FROM Games ORDER BY StartTime DESC)";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    for (int i = 0; i < FIELD_SIZE; i++)
                    {
                        for (int j = 0; j < FIELD_SIZE; j++)
                        {
                            command.Parameters.AddWithValue($"@Field{i * FIELD_SIZE + j}", field[i, j]);
                        }
                    }
                    command.ExecuteNonQuery();
                }
            }
        }

        // Начало новой игры
        private void StartNewGame()
        {
            // Заполнение игрового поля случайными числами
            Random random = new Random();
            List<int> numbers = Enumerable.Range(1, FIELD_SIZE * FIELD_SIZE - 1).ToList(); // Убираем 1 число 
            numbers.Shuffle(numbers);

            int k = 0;
            for (int i = 0; i < FIELD_SIZE; i++)
            {
                for (int j = 0; j < FIELD_SIZE; j++)
                {
                    if (k < numbers.Count)  // Проверяем, не закончился ли список чисел
                    {
                        field[i, j] = numbers[k++];
                    }
                    else
                    {
                        field[i, j] = 0; // Добавляем пустую клетку
                        emptyRow = i;
                        emptyCol = j;
                    }
                }
            }

            // Сохранение новой игры
            SaveNewGame();

            // Перерисовка игрового поля
            CreateGameField();
        }

        private void SaveNewGame()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string query = "INSERT INTO Games (Field0, Field1, Field2, Field3, Field4, Field5, Field6, Field7, Field8, IsFinished, StartTime) VALUES (@Field0, @Field1, @Field2, @Field3, @Field4, @Field5, @Field6, @Field7, @Field8, @IsFinished, @StartTime)";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Field0", field[0, 0]);
                    command.Parameters.AddWithValue("@Field1", field[0, 1]);
                    command.Parameters.AddWithValue("@Field2", field[0, 2]);
                    command.Parameters.AddWithValue("@Field3", field[1, 0]);
                    command.Parameters.AddWithValue("@Field4", field[1, 1]);
                    command.Parameters.AddWithValue("@Field5", field[1, 2]);
                    command.Parameters.AddWithValue("@Field6", field[2, 0]);
                    command.Parameters.AddWithValue("@Field7", field[2, 1]);
                    command.Parameters.AddWithValue("@Field8", field[2, 2]);
                    command.Parameters.AddWithValue("@IsFinished", false);
                    command.Parameters.AddWithValue("@StartTime", DateTime.Now);

                    command.ExecuteNonQuery();
                }
            }
        }

        // Проверка на победу
        private bool IsGameWon()
        {
            int k = 1;
            for (int i = 0; i < FIELD_SIZE; i++)
            {
                for (int j = 0; j < FIELD_SIZE; j++)
                {
                    if (field[i, j] != k)
                    {
                        return false;
                    }
                    k++;
                }
            }

            return true;
        }



        private void buttonNewGame_Click(object sender, RoutedEventArgs e)
        {
            StartNewGame();

        }

        private void buttonLoadGame_Click(object sender, RoutedEventArgs e)
        {
            CreateTable();
            LoadGame();
        }

        private void CreateTable()
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var tableExistsCommand = new SqlCommand(
                            "IF OBJECT_ID('dbo.Games') IS NULL " +
                            "BEGIN " +
                            "CREATE TABLE Games (" +
                            "Id INT PRIMARY KEY IDENTITY(1,1), " +
                            "Field0 INT, " +
                            "Field1 INT, " +
                            "Field2 INT, " +
                            "Field3 INT, " +
                            "Field4 INT, " +
                            "Field5 INT, " +
                            "Field6 INT, " +
                            "Field7 INT, " +
                            "Field8 INT, " +
                            "IsFinished BIT, " +
                            "StartTime DATETIME, " +
                            ") " +
                            "END", connection);
                tableExistsCommand.ExecuteNonQuery();
                connection.Close();
            }
        }


    }
}
