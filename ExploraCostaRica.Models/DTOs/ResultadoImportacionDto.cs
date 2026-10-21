
namespace ExploraCostaRica.Models.DTOs
{
    public class ResultadoImportacionDto
    {
        public bool Exitoso { get; set; }

        public string Mensaje { get; set; } = "";

        public long? MediaId { get; set; }

        public static ResultadoImportacionDto Exito(
            string mensaje,
            long mediaId)
        {
            return new ResultadoImportacionDto
            {
                Exitoso = true,
                Mensaje = mensaje,
                MediaId = mediaId
            };
        }

        public static ResultadoImportacionDto Error(
            string mensaje)
        {
            return new ResultadoImportacionDto
            {
                Exitoso = false,
                Mensaje = mensaje
            };
        }
    }
}
