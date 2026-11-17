# Awake ☕ para Windows

Tu equipo sigue trabajando mientras te tomas un café. Aplicación nativa para la bandeja del sistema, inspirada en [Despierto de laulys](https://github.com/laulys/despierto), reimplementada en C# y Windows Forms. Se conserva la licencia MIT y la atribución del original.

## Descargar y usar

**[Descargar Awake-Windows-x64.zip](https://github.com/ZmasterC39/awake/releases/latest/download/Awake-Windows-x64.zip)** · [Versiones publicadas](https://github.com/ZmasterC39/awake/releases) · [Estado de compilación](https://github.com/ZmasterC39/awake/actions)

1. Necesitas Windows 10 u 11 de 64 bits (x64).
2. Descarga el ZIP, extráelo en una carpeta estable y abre `Awake.exe`. No requiere instalar .NET ni permisos de administrador.
3. Elige 15 min, 30 min, 1 h, 2 h o **Sin límite**.
4. Cierra la ventana para dejar la app funcionando junto al reloj. Haz doble clic en la taza para volver a abrirla; también puedes usar el menú del botón derecho.
5. Pulsa **Volver a normal** o **Salir** para liberar la solicitud de energía.

La descarga aparece cuando GitHub Actions termina correctamente. Si aún no hay versión, revisa Actions. El ejecutable no tiene firma digital: Windows puede mostrar SmartScreen. Revisa el origen antes de ejecutarlo.

## Funciones

- Evita la suspensión automática por inactividad mediante `SetThreadExecutionState`, sin simular movimientos del ratón ni pulsaciones.
- Temporizador con cuenta atrás en la ventana, menú y texto del icono. Windows vuelve a gestionar la suspensión al terminar.
- Opción independiente para mantener encendida la pantalla.
- Batería visible y desactivación por debajo del 10 % sin alimentación externa. No permite activar una pausa en esa condición.
- Aviso al detectar el proceso `claude` mientras Awake está inactivo (una vez por ejecución).
- Acceso a Claude Code en el navegador y botón que copia `/remote-control` para activar manualmente una sesión desde Claude Code. No lee conversaciones ni credenciales; no genera QR ni descubre sesiones automáticamente.
- Inicio con Windows opcional, solo para el usuario actual. Al iniciar, permanece en modo normal hasta que actives una pausa.
- Una sola instancia por sesión de Windows.

## Tapa cerrada: configuración necesaria

**Awake no cambia la acción de cierre de tapa.** En Windows una solicitud de energía evita la suspensión por inactividad, pero no anula el cierre de tapa, la suspensión manual ni las políticas del administrador.

Si necesitas trabajar con la tapa cerrada:

1. Pulsa **Configurar cierre de tapa en Windows**.
2. En **Al cerrar la tapa**, selecciona **No hacer nada**, preferentemente solo con corriente alterna.
3. Guarda y activa una pausa en Awake.
4. Cuando ya no lo necesites, vuelve a esa pantalla y restaura **Suspender**.

**El ajuste de tapa es permanente e independiente del temporizador.** Al terminar la pausa, Awake libera su solicitud; Windows aplicará sus tiempos habituales de suspensión, no necesariamente se dormirá de inmediato. El hardware, Modern Standby y las políticas corporativas pueden limitar el comportamiento. Verifica primero con tu portátil conectado y una tarea de prueba.

No guardes el portátil activo en una mochila ni bloquees su ventilación. La protección por batería libera la solicitud de Awake; no cambia el ajuste de tapa ni fuerza la suspensión.

## Diferencias respecto a Despierto para Mac

| Función | Awake para Windows |
|---|---|
| Barra de menú | Bandeja del sistema junto al reloj |
| Evitar suspensión por inactividad | Sí, API nativa de Windows |
| Trabajar con tapa cerrada | Requiere ajustar Windows manualmente |
| Restaurar el cierre de tapa al acabar | No; el ajuste lo gestiona el usuario |
| Temporizador después de salir | No; salir libera inmediatamente la solicitud |
| Cristal animado de macOS | Interfaz nativa sencilla con icono de taza |
| Sesiones móviles y QR automático | Enlace general y comando copiado; sin QR |

No es una aplicación web y no necesita GitHub Pages. GitHub hospeda el código y genera el ejecutable descargable.

## Compilar

Instala el SDK de .NET 8. En Windows:

```powershell
git clone https://github.com/ZmasterC39/awake.git
cd awake
dotnet run --project tests/Awake.Tests -c Release
dotnet run --project src/Awake
```

Para crear el ejecutable autónomo:

```powershell
dotnet publish src/Awake/Awake.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o artifacts/win-x64
```

El workflow ejecuta pruebas de temporización/batería y compila en Windows. Cada cambio en `main` publica una nueva release y actualiza la descarga más reciente; el historial de artefactos de cada compilación queda en Actions. `SHA256SUMS.txt` permite verificar el ZIP descargado.

## Comprobación manual en Windows

- Activar una pausa y comprobar la solicitud con `powercfg /requests`.
- Volver a normal y comprobar que la solicitud desaparece.
- Cerrar la ventana y comprobar que el icono sigue activo; salir y comprobar que termina el proceso.
- Con una pausa de prueba, verificar expiración y comportamiento real de suspensión según el plan de energía.
- Probar pantalla, inicio automático y aviso de Claude Code.
- Verificar la tapa por separado: es un ajuste del equipo, no una garantía de la app.

Para quitar el inicio automático, desmarca la opción antes de mover o borrar `Awake.exe`. También puedes desactivarlo desde Aplicaciones de inicio de Windows. Awake no guarda el modo activo entre ejecuciones y Windows elimina su solicitud de energía cuando termina el proceso.
