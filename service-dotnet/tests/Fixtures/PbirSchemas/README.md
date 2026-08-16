# Pinned Microsoft Modern PBIR Schemas

These test-only fixtures are exact bytes from Microsoft json-schemas commit
34356d97e1218c79331780f8f5b77b03f2d13f35, retrieved on 2026-07-26.
Production code does not download, embed, or evaluate these schemas.

Source repository: https://github.com/microsoft/json-schemas

License: Microsoft json-schemas is distributed under the MIT License:
https://github.com/microsoft/json-schemas/blob/34356d97e1218c79331780f8f5b77b03f2d13f35/LICENSE

| Fixture | Canonical source | SHA-256 |
| --- | --- | --- |
| definitionProperties 2.0.0 | https://github.com/microsoft/json-schemas/blob/34356d97e1218c79331780f8f5b77b03f2d13f35/fabric/item/report/definitionProperties/2.0.0/schema.json | 1ea3450d1321a295abca6a9507548b4e2ec99ab11d3d4526aa73713650296ed0 |
| versionMetadata 1.0.0 | https://github.com/microsoft/json-schemas/blob/34356d97e1218c79331780f8f5b77b03f2d13f35/fabric/item/report/definition/versionMetadata/1.0.0/schema.json | 06f630c6741ae88dff0d80442295384ef38dca662811ef599a9365b144b3f0ac |
| report 1.0.0 | https://github.com/microsoft/json-schemas/blob/34356d97e1218c79331780f8f5b77b03f2d13f35/fabric/item/report/definition/report/1.0.0/schema.json | d73920133232bc5e8531d5e456d050b9e33469004509bbaa0de1a5a15c814319 |
| pagesMetadata 1.0.0 | https://github.com/microsoft/json-schemas/blob/34356d97e1218c79331780f8f5b77b03f2d13f35/fabric/item/report/definition/pagesMetadata/1.0.0/schema.json | e8a8803daee6d09927c5f4c303bef10cc9a70391db2960e36bba2055bde057ff |
| page 1.0.0 | https://github.com/microsoft/json-schemas/blob/34356d97e1218c79331780f8f5b77b03f2d13f35/fabric/item/report/definition/page/1.0.0/schema.json | 400bfc78e20d980e589d3a4d8e8890e9121e0a0356360ead370f5858f1b6d603 |
| visualContainer 1.0.0 | https://github.com/microsoft/json-schemas/blob/34356d97e1218c79331780f8f5b77b03f2d13f35/fabric/item/report/definition/visualContainer/1.0.0/schema.json | ebac0a74b3c4f1fd5a3497856a9a454eebd97b77ec22ff5c78765f919c8ff69b |
| formattingObjectDefinitions 1.0.0 | https://github.com/microsoft/json-schemas/blob/34356d97e1218c79331780f8f5b77b03f2d13f35/fabric/item/report/definition/formattingObjectDefinitions/1.0.0/schema.json | 1aaabab101bad35ac9fa28e5e0624512416d5c73b877d8dae5b798efc38c6974 |
| semanticQuery 1.0.0 | https://github.com/microsoft/json-schemas/blob/34356d97e1218c79331780f8f5b77b03f2d13f35/fabric/item/report/definition/semanticQuery/1.0.0/schema.json | 44ce4c731fbad24461af3735ba94483788e7769e0df23e8b49c387f90ba5b0df |

The report, page, visualContainer, and formattingObjectDefinitions schemas use
relative references to the two pinned dependency schemas. Tests register every
fixture locally and never resolve a network URL.
