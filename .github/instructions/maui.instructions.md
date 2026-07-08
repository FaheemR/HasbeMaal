---
description: "Use when working on .NET MAUI UI, Shell navigation, pages, view models, dependency injection, or platform-specific mobile code."
applyTo: ["src/Mobile/**/*.cs", "src/Mobile/**/*.xaml"]
---

# MAUI Guidelines

- Use Shell navigation with `ContentTemplate` so pages load on demand.
- Register Pages and ViewModels as transient services.
- Register shared app services as singletons.
- Keep Android SMS permission code behind interfaces.
- Do not put SMS parsing rules in MAUI pages or Android callbacks; call Core parser services.
- Keep UI placeholders simple until domain and storage flows are stable.