# Coding rules

- Each C# file must contain at most 250 lines. Split by responsibility, not to hide long functions.
- Use short, straightforward functions with clear names.
- Use English names for identifiers and English strings for tool instructions and error messages.
- Do not use object searches such as GameObject.Find, FindObjectOfType, FindObjectsOfType, FindFirstObjectByType, FindAnyObjectByType or GetObject.
- Do not use GetComponent, GetComponents, GetComponentInChildren, GetComponentsInChildren, GetComponentInParent or GetComponentsInParent, including TryGetComponent, unless the user explicitly permits them later.
- Assign scene/prefab/component references through serialized Inspector fields or pass them directly. Store references returned when creating objects/components; do not rediscover them through hierarchy traversal or names.
- Editor tools may load default prefab assets from explicit fixed paths, as authorized by the user. Preserve any custom Inspector assignment.
- Do not spawn subagents unless the user explicitly asks for delegation.
