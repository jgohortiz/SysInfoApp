# Política de Seguridad

## Versiones soportadas

| Versión | Soporte de seguridad |
|---|---|
| 1.x (actual) | Soportada |
| Anteriores | Sin soporte |

---

## Alcance

Esta política cubre el código fuente y el ejecutable distribuido de
SysInfoApp. La aplicación es una herramienta de solo lectura: consulta
información del sistema operativo y del hardware, pero no modifica
configuraciones, no escribe en el registro, no realiza conexiones de red
salientes y no almacena datos del usuario en ningún servidor externo.

---

## Modelo de amenazas

### Lo que la aplicación hace

- Lee información del sistema operativo mediante WMI y APIs de .NET.
- Lee el registro de Windows en modo de solo lectura.
- Enumera adaptadores de red, dispositivos y servicios del sistema.
- Genera un archivo Markdown local con la información recopilada.
- Puede ejecutarse por línea de comandos para automatizar la exportación.

### Lo que la aplicación NO hace

- No realiza conexiones de red salientes.
- No envía datos a servidores externos ni a servicios en la nube.
- No modifica el registro de Windows.
- No modifica configuraciones del sistema operativo.
- No requiere permisos de administrador para su funcionamiento normal.
- No instala servicios ni procesos en segundo plano.
- No persiste después de cerrarse.

---

## Superficie de ataque

| Vector | Exposición | Mitigación |
|---|---|---|
| Lectura de WMI | Solo lectura, sin escritura | try/catch en todas las consultas |
| Lectura del registro | Solo lectura en HKLM\SOFTWARE | try/catch por clave individual |
| Exportación a archivo | Solo escritura local en ruta elegida por el usuario | El usuario controla la ruta destino |
| Línea de comandos | Argumento --export con ruta de destino | Sanitización de nombre de archivo, creación controlada de carpeta |
| Fuentes de datos externas | netsh, wmic como fallback | Solo se ejecutan como procesos del sistema con permisos del usuario actual |

---

## Reportar una vulnerabilidad

Si descubres una vulnerabilidad de seguridad en SysInfoApp, por favor
repórtala de forma responsable antes de divulgarla públicamente.

### Cómo reportar

Envía un reporte por correo o mediante un Issue privado en el repositorio
con el siguiente formato:

**Asunto:** `[SECURITY] Descripción breve de la vulnerabilidad`

**Contenido del reporte:**

```
Versión afectada   :
Sistema operativo  :
Descripción        :
Pasos para reproducir :
Impacto estimado   :
Prueba de concepto (si aplica) :
```

### Dónde reportar

GitHub: https://github.com/jgohortiz

Para vulnerabilidades críticas que no deban exponerse públicamente,
crear un Issue con el prefijo [SECURITY] y marcar el contenido como
sensible, o contactar directamente al autor.

---

## Tiempos de respuesta esperados

| Actividad | Tiempo estimado |
|---|---|
| Confirmación de recepción del reporte | 5 días hábiles |
| Evaluación inicial de la vulnerabilidad | 10 días hábiles |
| Comunicación del plan de acción | 15 días hábiles |
| Publicación del parche (si aplica) | Según severidad |

---

## Clasificación de severidad

| Nivel | Descripción | Ejemplos |
|---|---|---|
| Crítico | Permite ejecución de código arbitrario o escalada de privilegios | Inyección de comandos vía argumentos CLI |
| Alto | Exposición de datos sensibles fuera del equipo local | Fuga de información a red externa |
| Medio | Comportamiento inesperado que afecta la integridad del reporte | Datos incorrectos en el archivo exportado |
| Bajo | Problema menor sin impacto en seguridad | Mensaje de error con información de depuración |

---

## Consideraciones de seguridad por componente

### Línea de comandos (`--export`, `--filename`)

El argumento `--filename` es sanitizado antes de usarse como nombre de
archivo. Se eliminan todos los caracteres inválidos para nombres de archivo
en Windows. Sin embargo, se recomienda no pasar valores provenientes de
fuentes no confiables directamente como argumento `--filename` en scripts
automatizados sin validación previa.

```bash
# Uso seguro en scripts
SysInfoApp.exe --export "C:\Reportes" --filename "equipo_01"

# Evitar pasar input de usuario sin validar
SysInfoApp.exe --export "C:\Reportes" --filename "%INPUT_EXTERNO%"
```

### Exportación de archivo

El archivo generado contiene información del sistema incluyendo hostname,
nombre de usuario, dirección IP, serial del equipo y lista de software
instalado. Se recomienda:

- Tratar el archivo exportado como información sensible.
- No compartirlo en canales públicos sin revisar su contenido.
- Al usarlo en scripts de inventario corporativo, asegurarse de que la
  carpeta de destino tenga permisos de escritura adecuados y acceso
  restringido a personal autorizado.

### Ejecución sin administrador

La aplicación está diseñada para funcionar sin privilegios elevados.
Ejecutarla como administrador no amplía su funcionalidad de forma
significativa. Se recomienda ejecutarla siempre con el nivel de privilegios
mínimo necesario (usuario estándar).

### Fallbacks de sistema (netsh, wmic)

Para obtener el SSID de Wi-Fi y el serial del BIOS cuando WMI no está
disponible, la aplicación ejecuta los procesos del sistema `netsh` y `wmic`
con los permisos del usuario actual. Estos procesos son parte del sistema
operativo Windows y no se redistribuyen con la aplicación.

---

## Distribución y verificación del ejecutable

### Hash del ejecutable

Antes de distribuir el ejecutable compilado, se recomienda generar y
publicar el hash SHA-256 para que los usuarios puedan verificar la
integridad del archivo.

```powershell
# Generar hash del ejecutable
Get-FileHash "SysInfoApp.exe" -Algorithm SHA256
```

### Firma digital

Para entornos corporativos se recomienda firmar el ejecutable con un
certificado de firma de código para evitar alertas de Windows SmartScreen.
Ver la sección de firma digital en la documentación del proyecto.

---

## Historial de vulnerabilidades

No se han reportado vulnerabilidades de seguridad hasta la fecha.

---

## Reconocimientos

Se agradece a quienes reporten vulnerabilidades de forma responsable.
Los reportes válidos serán reconocidos en las notas de la versión
correspondiente, salvo que el investigador prefiera mantenerse anónimo.

---

## Version de esta politica

Version 1.0 — 2025
404q9l - FoxShell
https://github.com/jgohortiz
