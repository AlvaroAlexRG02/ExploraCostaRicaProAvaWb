namespace ExploraCostaRica.Models.DTOs
{

    public class WikimediaImagenDto
    {

        public string Titulo { get; set; } = "";


        public string Descripcion { get; set; } = "";


        public string UrlImagen { get; set; } = "";


        public string UrlPagina { get; set; } = "";

        public string Autor { get; set; } = "";


        public string Licencia { get; set; } = "";

        public string UrlLicencia { get; set; } = "";

        public DateTime? Fecha { get; set; }
    }
}
