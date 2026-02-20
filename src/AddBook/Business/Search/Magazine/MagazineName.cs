using System.ComponentModel.DataAnnotations;

namespace AddBook.Business.Search.Magazine
{
    public enum MagazineName
    {
        Epsiloon,
        [Display(Name = "La Revue Dessinée")]
        LaRevueDessinee,
        [Display(Name = "S!CK")]
        Sick,
        [Display(Name = "Inconnu")]
        Unknown,
        [Display(Name = "Galaxies")]
        Galaxies
    }
}
