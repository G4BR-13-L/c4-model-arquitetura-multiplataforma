use serde::{Deserialize, Serialize};
use std::fmt;
use sqlx::{
    Type, Postgres,
    decode::Decode,
    encode::{Encode, IsNull},
    postgres::PgTypeInfo,
};

#[derive(Debug, Clone, PartialEq, Eq, Serialize, Deserialize, Default)]
#[serde(transparent)]
pub struct EmailField(String);

impl EmailField {
    #[must_use]
    pub fn new(value: String) -> Result<Self, &'static str> {
        let value = value.trim();
        if value.is_empty() {
            return Err("Email cannot be empty");
        }
        if !value.contains('@') || !value.contains('.') {
            return Err("Email format is invalid");
        }
        Ok(Self(value.to_string()))
    }

    pub fn as_str(&self) -> &str {
        &self.0
    }
}

impl fmt::Display for EmailField {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        self.0.fmt(f)
    }
}


impl Type<Postgres> for EmailField {
    fn type_info() -> PgTypeInfo {
        <String as Type<Postgres>>::type_info()
    }
}

impl<'r> Decode<'r, Postgres> for EmailField {
    fn decode(
        value: <Postgres as sqlx::Database>::ValueRef<'r>,
    ) -> Result<Self, sqlx::error::BoxDynError> {
        let s = <String as Decode<Postgres>>::decode(value)?;
        Ok(Self(s))
    }
}

impl<'q> Encode<'q, Postgres> for EmailField {
    fn encode_by_ref(
        &self,
        buf: &mut <Postgres as sqlx::Database>::ArgumentBuffer<'q>,
    ) -> Result<IsNull, sqlx::error::BoxDynError> {
        <String as Encode<Postgres>>::encode_by_ref(&self.0, buf)
    }
}
