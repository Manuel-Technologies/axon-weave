from __future__ import annotations

from pathlib import Path
from typing import Any, BinaryIO

import requests


class AxonWeaveError(RuntimeError):
    def __init__(self, message: str, status_code: int, body: Any):
        super().__init__(message)
        self.status_code = status_code
        self.body = body


class AxonWeaveClient:
    def __init__(self, base_url: str, token: str | None = None, session: requests.Session | None = None):
        self.base_url = base_url.rstrip("/")
        self.token = token
        self.session = session or requests.Session()

    def set_token(self, token: str) -> None:
        self.token = token

    def register(self, phone_number: str, name: str) -> dict[str, Any]:
        return self._request("POST", "/api/auth/register", json={"phoneNumber": phone_number, "name": name})

    def verify_otp(self, phone_number: str, code: str) -> dict[str, Any]:
        auth = self._request("POST", "/api/auth/verify-otp", json={"phoneNumber": phone_number, "code": code})
        self.token = auth["token"]
        return auth

    def search_users(self, phone: str | None = None) -> list[dict[str, Any]]:
        params = {"phone": phone} if phone else None
        return self._request("GET", "/api/users", params=params)

    def create_conversation(self, participant_ids: list[str], is_group: bool = False, title: str | None = None) -> dict[str, Any]:
        return self._request("POST", "/api/conversations", json={
            "participantIds": participant_ids,
            "isGroup": is_group,
            "title": title
        })

    def list_conversations(self) -> list[dict[str, Any]]:
        return self._request("GET", "/api/conversations")

    def get_messages(self, conversation_id: str, before: str | None = None, limit: int = 50) -> list[dict[str, Any]]:
        params: dict[str, Any] = {"conversationId": conversation_id, "limit": limit}
        if before:
            params["before"] = before
        return self._request("GET", "/api/messages", params=params)

    def send_message(
        self,
        conversation_id: str,
        encrypted_content: str,
        media_url: str | None = None,
        media_content_type: str | None = None,
    ) -> dict[str, Any]:
        return self._request("POST", "/api/messages", json={
            "conversationId": conversation_id,
            "encryptedContent": encrypted_content,
            "mediaUrl": media_url,
            "mediaContentType": media_content_type
        })

    def delete_message(self, message_id: str) -> None:
        self._request("DELETE", f"/api/messages/{message_id}", unwrap=False)

    def mark_read(self, message_id: str, conversation_id: str) -> dict[str, Any]:
        return self._request("PUT", f"/api/messages/{message_id}/read", json={"conversationId": conversation_id})

    def upload_media(self, file: str | Path | BinaryIO, file_name: str | None = None) -> dict[str, Any]:
        if isinstance(file, (str, Path)):
            path = Path(file)
            with path.open("rb") as handle:
                return self._upload_file(handle, file_name or path.name)

        return self._upload_file(file, file_name or "upload")

    def _upload_file(self, handle: BinaryIO, file_name: str) -> dict[str, Any]:
        files = {"file": (file_name, handle)}
        return self._request("POST", "/api/media/upload", files=files)

    def _request(self, method: str, path: str, unwrap: bool = True, **kwargs: Any) -> Any:
        headers = kwargs.pop("headers", {})
        if self.token:
            headers["Authorization"] = f"Bearer {self.token}"

        response = self.session.request(method, f"{self.base_url}{path}", headers=headers, timeout=30, **kwargs)
        if not response.ok:
            raise AxonWeaveError(f"axon-weave request failed with {response.status_code}", response.status_code, self._read_body(response))

        if response.status_code == 204:
            return None

        body = response.json()
        return body.get("data") if unwrap else body

    @staticmethod
    def _read_body(response: requests.Response) -> Any:
        try:
            return response.json()
        except ValueError:
            return response.text
