#!/bin/bash
if [ -z "$1" ]; then
  echo "❌ Migration name required. Example:"
  echo "./scripts/migrate.sh AddAuditIndex"
  exit 1
fi

echo "🚀 Creating migration: $1"
dotnet ef migrations add "$1" \
  --project ./src/NabdCare.Infrastructure \
  --startup-project ./src/NabdCare.API

echo "🔄 Updating database..."
dotnet ef database update \
  --project ./src/NabdCare.Infrastructure \
  --startup-project ./src/NabdCare.API

echo "✅ Migration complete!"