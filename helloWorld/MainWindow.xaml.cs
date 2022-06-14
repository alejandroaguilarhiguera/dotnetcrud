using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using System.Web.Script.Serialization;
namespace helloWorld
{
    class ProductModel
    {
        const string EXTERNAL_API = "http://192.168.0.2:1337";
        const string MEDIA_TYPE = "application/json";
        const string PREFIX = "/api"; // or "/"
        const string ENDPOINT_OF_PRODUCTS = "/products";

        static HttpClient client = new HttpClient();
        
        public static async Task<List<Product>> getAllProducts()
        {
            List<Product> products = new List<Product>();

            var httpResponse = await client.GetAsync(EXTERNAL_API + PREFIX+ ENDPOINT_OF_PRODUCTS, HttpCompletionOption.ResponseHeadersRead);
            httpResponse.EnsureSuccessStatusCode();
            if (httpResponse.Content is object && httpResponse.Content.Headers.ContentType.MediaType == MEDIA_TYPE)
            {
                string responseInString = await httpResponse.Content.ReadAsStringAsync();
                var data = JObject.Parse(responseInString)["data"];
                foreach (var item in data)
                {

                    Product product = new Product()
                    {
                        Id = int.Parse(item["id"].ToString()),
                        Name = item["attributes"]["name"].ToString(),
                        Description= item["attributes"]["description"].ToString(),

                        Price= float.Parse(item["attributes"]["price"].ToString())
                    };
                    products.Add(product);
                }
                return products;
            }
            else
            {

            }
            return null;
        }

        public static async Task<Product> create(Product newProduct)
        {
            var serializer = new JavaScriptSerializer();
            var serializedResult = serializer.Serialize(new { data = new { name = newProduct.Name, description = newProduct.Description, price = newProduct.Price } });
            var content = new StringContent(serializedResult, Encoding.UTF8, "application/json");
            
            var httpResponse = await client.PostAsync(EXTERNAL_API + PREFIX + ENDPOINT_OF_PRODUCTS, content);
            httpResponse.EnsureSuccessStatusCode();
            if (httpResponse.Content is object && httpResponse.Content.Headers.ContentType.MediaType == MEDIA_TYPE)
            {
                string responseInString = await httpResponse.Content.ReadAsStringAsync();
                var data = JObject.Parse(responseInString)["data"];

                Product product = new Product()
                {
                    Id = int.Parse(data["id"].ToString()),
                    Name = data["attributes"]["name"].ToString(),
                    Description = data["attributes"]["description"].ToString(),
                    Price = float.Parse(data["attributes"]["price"].ToString()),
                };
                return product;
            }

            return null;
        }


        public static async Task<Product> update(Product currentProduct)
        {
            var serializer = new JavaScriptSerializer();
            var serializedResult = serializer.Serialize(new { data = new { name = currentProduct.Name, description = currentProduct.Description, price = currentProduct.Price } });
            var content = new StringContent(serializedResult, Encoding.UTF8, "application/json");

            var httpResponse = await client.PutAsync(EXTERNAL_API + PREFIX + ENDPOINT_OF_PRODUCTS + "/" + currentProduct.Id, content);
            httpResponse.EnsureSuccessStatusCode();
            if (httpResponse.Content is object && httpResponse.Content.Headers.ContentType.MediaType == MEDIA_TYPE)
            {
                string responseInString = await httpResponse.Content.ReadAsStringAsync();
                var data = JObject.Parse(responseInString)["data"];

                Product product = new Product()
                {
                    Id = int.Parse(data["id"].ToString()),
                    Name = data["attributes"]["name"].ToString(),
                    Description = data["attributes"]["description"].ToString(),
                    Price = float.Parse(data["attributes"]["price"].ToString()),
                };
                return product;
            }

            return null;
        }

        public static async Task<bool> delete(int productId)
        {
            
            var httpResponse = await client.DeleteAsync(EXTERNAL_API + PREFIX + ENDPOINT_OF_PRODUCTS + "/" + productId);
            httpResponse.EnsureSuccessStatusCode();
            if (httpResponse.Content is object && httpResponse.Content.Headers.ContentType.MediaType == MEDIA_TYPE)
            {
                return true;
            }

            return false;
        }
    }
    class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public float Price{ get; set; }
       
        public Product()
        {

        }

    }


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        int? productIdSelected = null;
        public MainWindow()
        {   
            InitializeComponent();
        }

        async void OnLoad(object sender, RoutedEventArgs e)
        {
            var products = await ProductModel.getAllProducts();
            
            foreach(Product product in products)
            {
              lvProducts.Items.Add(product);

            }
        }

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            Product productToSave = new Product()
            {
                Name = txtName.Text,
                Description = txtDescription.Text,
                Price = float.Parse(txtPrice.Text)
            };

            Product currentProduct = null;
            if (productIdSelected == null)
            { // save a new product
                currentProduct = await ProductModel.create(productToSave);
            }
            else
            { // update
                productToSave.Id = int.Parse(productIdSelected.ToString());
                currentProduct = await ProductModel.update(productToSave);
            }

            lvProducts.Items.Add(currentProduct);
            if (currentProduct != null)
            {
                txtName.Text = "";
                txtDescription.Text = "";
                txtPrice.Text = "";
                MessageBox.Show("Se guardo un nuevo producto");
            }
            productIdSelected = null;
        }

        private void lvProducts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                if (lvProducts.SelectedItem != null)
                {
                    btnDelete.IsEnabled = true;
                }
            }
        }

        private async void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            Product productSelected = (Product)lvProducts.SelectedValue;
            if (productSelected == null)
            {
                return;
            }
            bool isSuccess = await ProductModel.delete(productSelected.Id);
            if (!isSuccess)
            { // Error
                MessageBox.Show("Ocurrio un error al intentar eliminar");
            }
            if (isSuccess)
            {
                lvProducts.Items.Remove(lvProducts.SelectedItem);
                btnDelete.IsEnabled = false;
                productIdSelected = null;
            }
        }

        private void lvProducts_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Product productSelected = (Product)lvProducts.SelectedValue;
            productIdSelected = productSelected.Id;
            txtName.Text = productSelected.Name;
            txtDescription.Text = productSelected.Description;
            txtPrice.Text = productSelected.Price.ToString();
            lvProducts.Items.Remove(lvProducts.SelectedItem);
            btnDelete.IsEnabled = true;
        }
    }
}
