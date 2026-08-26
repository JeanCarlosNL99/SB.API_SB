/**
 * Logotipo institucional dibujado como SVG.
 *
 * Se dibuja en linea, sin archivo de imagen, para que el logotipo escale sin
 * perder nitidez y para que la aplicacion no dependa de descargar un recurso
 * externo. Si se dispone del archivo oficial del portal de la Superintendencia,
 * basta sustituir el contenido de este componente por la etiqueta de imagen
 * correspondiente: ningun otro archivo cambia.
 */
export function BrandLogo({
  variant = 'light',
  className = 'brand-logo',
}: {
  variant?: 'light' | 'dark';
  className?: string;
}) {
  const primaryColor = variant === 'light' ? '#ffffff' : 'rgb(13, 48, 72)';
  const secondaryColor = variant === 'light' ? 'rgba(255, 255, 255, 0.62)' : 'rgb(108, 138, 160)';

  return (
    <svg
      className={className}
      viewBox="0 0 320 112"
      role="img"
      aria-label="Superintendencia de Bancos, Republica Dominicana"
      xmlns="http://www.w3.org/2000/svg"
    >
      <g>
        {/* Gota institucional que precede al monograma. */}
        <path
          d="M8 12c0 18 10 30 24 34 9 3 17-2 17-11 0-7-5-11-13-15L8 12z"
          fill={primaryColor}
        />
        <path
          d="M26 4c14 0 26 3 34 8-3 8-9 13-16 13-8 0-15-5-21-13L26 4z"
          fill={secondaryColor}
        />
        {/* Monograma SB. */}
        <text
          x="70"
          y="52"
          fill={primaryColor}
          fontFamily="'Segoe UI', Helvetica, Arial, sans-serif"
          fontSize="58"
          fontWeight="700"
          letterSpacing="-1"
        >
          SB
        </text>
      </g>
      <text
        x="0"
        y="80"
        fill={primaryColor}
        fontFamily="'Segoe UI', Helvetica, Arial, sans-serif"
        fontSize="19"
        fontWeight="700"
        letterSpacing="1.2"
      >
        SUPERINTENDENCIA
      </text>
      <text
        x="0"
        y="98"
        fill={primaryColor}
        fontFamily="'Segoe UI', Helvetica, Arial, sans-serif"
        fontSize="19"
        fontWeight="700"
        letterSpacing="1.2"
      >
        DE BANCOS
      </text>
      <text
        x="0"
        y="110"
        fill={secondaryColor}
        fontFamily="'Segoe UI', Helvetica, Arial, sans-serif"
        fontSize="9"
        fontWeight="600"
        letterSpacing="2.6"
      >
        REPÚBLICA DOMINICANA
      </text>
    </svg>
  );
}
