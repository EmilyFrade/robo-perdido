using UnityEngine;

namespace RoboPerdido
{
    /// <summary>
    /// Paleta de cores oficial do projeto (definida na Etapa 5 - Identidade Visual).
    /// Centralizar as cores aqui mantem o prototipo coerente com a direcao de arte,
    /// mesmo usando apenas placeholders geometricos.
    /// </summary>
    public static class GameColors
    {
        static Color Hex(string h)
        {
            ColorUtility.TryParseHtmlString(h, out Color c);
            return c;
        }

        public static readonly Color AcoBase  = Hex("#1E2A35"); // fundo / piso
        public static readonly Color Ferrugem = Hex("#8B3A1A"); // acento, setor Montagem
        public static readonly Color Ambar    = Hex("#D4922A"); // energia, interativos
        public static readonly Color Acido     = Hex("#4B7C3F"); // perigo / corrosivo
        public static readonly Color Papel      = Hex("#D4C9B0"); // UI clara / textos
        public static readonly Color Robo       = Hex("#9AA7B2"); // corpo do M-37
        public static readonly Color Parede     = Hex("#33414D"); // paredes da fabrica
    }
}
