# Servidor MCP

La API expone un servidor [MCP](https://modelcontextprotocol.io) en `POST /mcp`. Con él, un agente
(Claude Code u otro cliente MCP) gestiona los formularios y envíos de LeadHub en nombre del usuario
que ha iniciado sesión, con su misma cuenta.

## Diseño

- **Una tool por endpoint de administración.** Las tools viven en `backend/LeadHub.Api/Mcp/` y
  llaman a los mismos puertos de entrada que los controladores (`IAdminForms`,
  `IAdminSubmissions`). No contienen lógica de negocio. SDK oficial de C#
  (`ModelContextProtocol.AspNetCore`), transporte HTTP streamable sin estado.
- **Misma autenticación que el panel.** `/mcp` exige un access token OIDC (JWT) emitido por
  `Auth:Authority` con audiencia `Auth:Audience`, validado por el mismo esquema `JwtBearer`. El
  propietario sale del claim `sub`, así que el agente ve y crea exactamente lo mismo que el usuario
  en el panel.
- **Descubrimiento OAuth.** Una petición anónima recibe `401` con
  `WWW-Authenticate: Bearer resource_metadata=...`. El documento
  `/.well-known/oauth-protected-resource/mcp` (RFC 9728) apunta a `Auth:Authority`, y desde ahí el
  cliente MCP inicia el login en el navegador. No hay credenciales de máquina ni API keys: el
  cliente guarda y renueva los tokens.
- **Errores.** Un fallo de caso de uso devuelve `isError: true` con el texto
  `"<status> <CODE>: <mensaje>"`, el mismo status y código que daría el endpoint REST (p. ej.
  `404 FORM_NOT_FOUND: ...`).
- **JSON.** Igual que la API REST: enums por nombre (`"Spam"`) y campos vacíos como `null`.

## Tabla endpoint → tool

| Endpoint REST | Tool MCP | Tipo |
|---|---|---|
| `GET /api/admin/forms` | `list_forms` | lectura |
| `GET /api/admin/forms/{formId}` | `get_form` | lectura |
| `POST /api/admin/forms` | `create_form` | escritura |
| `PUT /api/admin/forms/{formId}` | `update_form` | escritura (parcial: los campos omitidos no cambian; `""` borra `description`, `notificationEmail` y `thankYouUrl`) |
| `DELETE /api/admin/forms/{formId}` | `delete_form` | destructiva |
| `GET /api/admin/forms/{formId}/submissions` | `list_submissions` | lectura (filtro `status`) |
| `GET /api/admin/submissions/{submissionId}` | `get_submission` | lectura |
| `PUT /api/admin/submissions/{submissionId}/spam` | `mark_submission` | escritura |

El endpoint público de envío (`POST /api/forms/{formSlug}/submit`) no tiene tool.

`McpServerTests.Every_admin_endpoint_has_an_mcp_tool` falla si se añade una acción a un
controlador de administración sin su tool.

## Configuración del backend

| Clave | Obligatoria | Descripción |
|---|---|---|
| `Auth:Authority` | sí (ya existe) | Proveedor OIDC. Se publica como `authorization_servers`. |
| `Auth:Audience` | sí (ya existe) | Audiencia del JWT. Zitadel incluye en el token los client id de todas las apps del proyecto, así que vale para la app del cliente MCP. |
| `Mcp:ResourceUrl` | recomendada en producción | URL pública del endpoint, p. ej. `https://leadhub-backend.ne2.studio/mcp`. Si falta, se infiere de la petición. |
| `Mcp:Scopes` | no | Scopes anunciados. Por defecto `openid`, `profile`, `offline_access`. |

Como variables de entorno: `Mcp__ResourceUrl`, `Mcp__Scopes__0`, ... En producción las fija
Terraform (`modules/leadhub` del repo `infra`).

## Tests

`backend/LeadHub.Api.Tests/McpServerTests.cs` conecta un cliente MCP real contra un `TestServer`
con los casos de uso reales y repositorios en memoria. Cubre el inventario y las anotaciones de las
tools, el challenge `401` con la metadata, el recorrido completo de formularios y envíos, los
errores con su status y código, el aislamiento entre propietarios y los tokens sin `sub`.

```bash
cd backend && dotnet test
```

---

# Usar LeadHub desde Claude Code

## 1. Administrador: cliente OIDC en Zitadel (una sola vez)

La app `LeadHub MCP` está en Terraform (`modules/leadhub/zitadel.tf` del repo `infra`), dentro del
proyecto `LeadHub - Prod` para que el token lleve la audiencia que espera la API. Es una app nativa
pública: PKCE, sin client secret, token de acceso JWT y refresh token. Su redirect es
`http://localhost:3119/callback` (3118 es el de CashClarity).

Su client id no es un secreto:

```bash
terraform output -raw leadhub_mcp_oidc_client_id
```

Comprobar el backend:

```bash
curl https://leadhub-backend.ne2.studio/.well-known/oauth-protected-resource/mcp
```

Debe incluir `"authorization_servers": ["https://auth.ne2.studio"]` y
`"resource": "https://leadhub-backend.ne2.studio/mcp"`.

## 2. Usuario: conectar Claude Code

```bash
claude mcp add --transport http \
  --client-id <CLIENT_ID> \
  --callback-port 3119 \
  --scope user \
  leadhub https://leadhub-backend.ne2.studio/mcp
```

- `--scope user` deja el servidor disponible en todos tus proyectos.
- No hay que guardar ninguna credencial: Claude Code guarda los tokens en su almacén y los renueva.

Iniciar sesión: en Claude Code, `/mcp` → `leadhub` → **Authenticate**. Se abre el login de Zitadel
en el navegador. Al volver, `leadhub` aparece como *connected* con 8 tools.

**Renovar la sesión.** Dura lo que permita el refresh token de Zitadel. Si una tool falla con `401`
o el servidor aparece como *needs authentication*, repite `/mcp` → `leadhub` → **Authenticate**.
Un agente sin navegador (CI) no puede hacer este login.

`delete_form` está marcada como destructiva: Claude Code pide confirmación antes de usarla.

## Problemas frecuentes

| Síntoma | Causa probable | Solución |
|---|---|---|
| Zitadel: `redirect_uri` no válida | Puerto distinto del registrado | Usa `--callback-port 3119`. |
| Login correcto, pero las tools dan `401` | Token opaco o app en otro proyecto | Auth Token Type `JWT` y app en `LeadHub - Prod`. |
| Claude Code no abre el navegador | No recibe el `WWW-Authenticate` | `curl -i -X POST https://leadhub-backend.ne2.studio/mcp` debe dar `401` con `resource_metadata`. |
| La metadata muestra `http://` en `resource` | Falta `Mcp__ResourceUrl` | Defínela en el despliegue. |
