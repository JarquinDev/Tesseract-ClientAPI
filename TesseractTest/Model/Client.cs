using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace TesseractTest.Model
{
    public class Client
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Lastname1 { get; set; }
        public string Lastname2 { get; set; }
        public string Cedula {  get; set; }

        public Client()
        {
        }
        public Client(string name, string lastname1, string lastname2, string cedula)
        {
            Name = name;
            Lastname1 = lastname1;
            Lastname2 = lastname2;
            Cedula = cedula;
        }
    }

}
