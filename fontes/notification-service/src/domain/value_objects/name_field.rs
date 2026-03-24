use serde::{Deserialize, Serialize};
use std::fmt;
use sqlx::{
    Type, Postgres,
    decode::Decode,
    encode::{Encode, IsNull},
    postgres::PgTypeInfo,
};

#[derive(Debug, Clone, PartialEq, Eq, Serialize, Default)]
#[serde(transparent)]
pub struct NameField(String);

impl NameField {
    #[must_use]
    pub fn new(value: String) -> Result<Self, &'static str> {
        let value = value.trim();
        if value.is_empty() {
            return Err("Name cannot be empty");
        }
        if value.len() > 253 {
            return Err("Name should have maximum 255 characters");
        }
        Ok(Self(value.to_string()))
    }

    pub fn as_str(&self) -> &str {
        &self.0
    }
}

impl<'de> serde::Deserialize<'de> for NameField {
    fn deserialize<D>(deserializer: D) -> Result<Self, D::Error>
    where
        D: serde::Deserializer<'de>,
    {
        let s = String::deserialize(deserializer)?;
        Self::new(s).map_err(serde::de::Error::custom)
    }
}

impl fmt::Display for NameField {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        self.0.fmt(f)
    }
}

impl Type<Postgres> for NameField {
    fn type_info() -> PgTypeInfo {
        <String as Type<Postgres>>::type_info()
    }
}

impl<'r> Decode<'r, Postgres> for NameField {
    fn decode(
        value: <Postgres as sqlx::Database>::ValueRef<'r>,
    ) -> Result<Self, sqlx::error::BoxDynError> {
        let s = <String as Decode<Postgres>>::decode(value)?;
        Ok(Self(s))
    }
}

impl<'q> Encode<'q, Postgres> for NameField {
    fn encode_by_ref(
        &self,
        buf: &mut <Postgres as sqlx::Database>::ArgumentBuffer<'q>,
    ) -> Result<IsNull, sqlx::error::BoxDynError> {
        <String as Encode<Postgres>>::encode_by_ref(&self.0, buf)
    }
}
