using Microsoft.AspNetCore.Components;
using SearchApp.Models;
using SearchApp.Services;

namespace SearchApp.Pages
{
    public partial class CustomerDialog : ComponentBase
    {
        [Parameter] public Customer Customer { get; set; } = new Customer();
        [Parameter] public EventCallback<bool> OnClose { get; set; }
        [Parameter] public string? customerId { get; set; }

        [Inject]
        public MongoDbService? mongoDbService
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
            Customer = new Customer { };
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
            if (Customer.id == null)
            {
                Customer.id = Guid.NewGuid().ToString();
                await mongoDbService.InsertCustomer(Customer);
                return OnClose.InvokeAsync(true);
            }
            await mongoDbService.UpdateDocument<Customer>("customer", Customer, Customer.id);
            return OnClose.InvokeAsync(true);
        }
    }
}