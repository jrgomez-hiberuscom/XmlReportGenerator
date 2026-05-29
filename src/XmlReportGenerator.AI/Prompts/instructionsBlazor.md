### **Creación de plantillas *Razor***

** Modelo **
A partir del json suministrado, se debe generar un modelo para deserializarlo.
- Las fechas serán Datetime
- El resto string




> ⚠️ **Aviso importante**
>
> No deben incluirse etiquetas globales de *HTML* como <code>&lt;html&gt;</code>, <code>&lt;head&gt;</code> o <code>&lt;body&gt;</code> en la plantilla. Incluirlas puede provocar conflictos durante el procesado y la generación del reporte. *Report@* se encarga automáticamente de añadir la estructura completa del documento, por lo que únicamente debe definirse el contenido específico del reporte.

La definición de un nuevo reporte requiere añadir un proyecto a la solución, en el que se incluirá la plantilla <code>.razor</code> y, si es necesario, otros componentes asociados.

Una vez creado el proyecto, la plantilla <code>.razor</code> se puede construir a partir del *HTML* obtenido en la conversión de un documento existente o diseñarse directamente desde cero, en los casos en los que se trate de un reporte nuevo sin un documento de referencia.

#### **Convención de nombres del proyecto y de la plantilla .razor**

Para mantener la coherencia y evitar colisiones, tanto el proyecto como la plantilla deben seguir una convención de nombres que permita identificarlos de manera única en Report@.

- **Proyecto**

  El nombre del proyecto debe seguir el formato:

  <code>Reporta.Templates.{Servicio/Sección/Departamento/Grupo}.{Producto}.{Reporte}</code>
  
  > 📌 **Reporte de ejemplo**
  > 
  > Para un reporte de diligencia del producto *RAI* en el departamento de *Hacienda*:
  >
  > <code>Reporta.Templates.Hfn.Rai.Diligencia</code>

- **Plantilla .razor**

  El nombre de la plantilla debe seguir el formato:

  <code>{Servicio/Sección/Departamento/Grupo}{Producto}{Reporte}.razor</code>

  > 📌 **Reporte de ejemplo**
  > 
  > Para el mismo caso anterior (reporte de diligencia del producto *RAI* en *Hacienda*):
  >
  > <code>HfnRaiDiligencia.razor</code>

#### **Plantilla base**

Todas las plantillas deben heredar de la clase base <code>SsidArqNetReportBase&lt;TModel&gt;</code>, donde <code>TModel</code> corresponde al modelo de datos deserializado a partir del *JSON* recibido:

```razor
@using SsidArqNet.Components.Blazor.Reporta.Components

<!-- Todos los reportes deben heredar de la clase SsidArqNetReportBase -->

@inherits SsidArqNetReportBase<TModel>

<!-- Aquí comienza el reporte -->

...
```

#### **Definición de estilos dentro del &lt;head&gt; del reporte** (opcional)

> ⚠️ **Aviso importante**
> 
> No deben añadirse etiquetas <code>&lt;head&gt;</code> manualmente en la plantilla, ya que hacerlo provocaría conflictos con el motor de *Report@*.

El motor de renderización de *Report@* permite que cada plantilla *Razor* defina estilos personalizados en la sección <code>&lt;head&gt;</code> del documento generado, utilizando para ello la sección con nombre <code>@HeadContentSectionId</code>:

```razor
...

<SectionContent SectionName="@HeadContentSectionId">

    <style type="text/css">

        .claseCss {
          ...
        }

    </style>

</SectionContent>
```

#### **Maquetación en páginas** (opcional)

> ⚠️ **Aviso importante**
> 
> El uso del componente <code>&lt;SsidArqNetReportPage&gt;</code> implica que, tras su inserción, se forzará un salto de página en el archivo final generado.

Para estructurar el reporte en páginas, desde el *GAT-SSID* se recomienda encapsular el contenido dentro de la etiqueta <code>&lt;SsidArqNetReportPage&gt;</code>:

```razor
<SsidArqNetReportPage>

  Contenido de la página

</SsidArqNetReportPage>
```

Cada bloque de contenido definido dentro de esta etiqueta corresponderá con una página del reporte:

![Salto de página en TemplateBuilder](./Imgs/TemplateBuilderHtmlPageBreak.png)

En caso de que el espacio disponible no sea suficiente, el contenido restante se trasladará automáticamente a la página siguiente. Este comportamiento puede comprobarse de antemano al renderizar el reporte en formato *HTML* mediante *Report@ TemplateBuilder*:

![Desbordamiento de página en TemplateBuilder](./Imgs/TemplateBuilderHtmlPageOverflow.png)

Ejemplo de maquetación:

```razor
@using SsidArqNet.Components.Blazor.Reporta.Components

<!-- Todos los reportes deben heredar de la clase SsidArqNetReportBase -->

@inherits SsidArqNetReportBase<TModel>

<!-- Aquí comienza el reporte -->

<SsidArqNetReportPage>

  Contenido de la página 1

</SsidArqNetReportPage>

<SsidArqNetReportPage>

  Contenido de la página 2

</SsidArqNetReportPage>

...
```
Se incorpora una plantilla de Ejemplo
Se generaran los campos del modelo en función de los campos del json
Se deben hacer corresponder los valores del json con los valores y etiquetas del html para hallar donde va cada campo
El contenido del informe debe entrar en páginas A4 con margenes superior 1.5, inferior 1.5, izquierdo 2.5, derecho 1
Se debe evitar utilizar flex ni nada que no sea compatible con el motor de renderizado Wkhtmltopdf
