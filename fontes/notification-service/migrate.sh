export DATABASE_URL="postgres://postgres:postgres@localhost:5432/notification_service"
export SQLX_OFFLINE=false
sqlx migrate run
rm -rf .sqlx/
cargo sqlx prepare -- --all-targets
git add .sqlx/
