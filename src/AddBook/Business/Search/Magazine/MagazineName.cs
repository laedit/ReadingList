using System.ComponentModel.DataAnnotations;

namespace AddBook.Business.Search.Magazine
{
    // FIXME remplacer par un name sur IMagazineSearch et utiliser l'injection de dépendances pour s'éviter de devoir renseigner
    // un nouveau magazine à plusieurs endroits ?
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
        Galaxies,
        [Display(Name = "JV le mag")]
        JVLeMag
    }
}
