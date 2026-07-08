namespace CreationStore.API.DTOs.ResponseTypes
{
     public class ResponseTypeDTO<T>
    {
        public int StatusCode { get; set; }

        public string? Message { get; set; }

        public T? Content { get; set; }

        public DateTime DateTime { get; set; } = System.DateTime.Now;
    }
}