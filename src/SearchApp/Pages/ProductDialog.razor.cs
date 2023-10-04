using Microsoft.AspNetCore.Components;
using SearchApp.Models;
using SearchApp.Services;

namespace SearchApp.Pages
{
    public partial class ProductDialog : ComponentBase
    {
        [Parameter] public Product? Product { get; set; } = new Product();
        [Parameter] public EventCallback<bool> OnClose { get; set; }
        [Parameter] public string? productId { get; set; }

        [Inject]
        public MongoDbService mongoDbService
        {
            get;
            set;
        }

        public bool ShowDialog
        {
            get;
            set;
        }

        [Parameter]
        public EventCallback<bool> CloseEventCallback
        {
            get;
            set;
        }

        public void Show()
        {
            ResetDialog();
            ShowDialog = true;
            StateHasChanged();
        }

        public void Close()
        {
            ShowDialog = false;
            StateHasChanged();
        }

        private void ResetDialog()
        {
            Product = new Product { };
        }

        private Task Cancel()
        {
            return OnClose.InvokeAsync(false);
        }

        private Task Ok()
        {
            return OnClose.InvokeAsync(true);
        }

        protected async Task<Task> HandleValidSubmit()
        {
            if (Product.id == null)
            {
                await mongoDbService.InsertProduct(Product);
                return OnClose.InvokeAsync(true);
            }
            await mongoDbService.UpdateDocument<Product>("product", Product, Product.id);
            return OnClose.InvokeAsync(true);
        }
    }
}