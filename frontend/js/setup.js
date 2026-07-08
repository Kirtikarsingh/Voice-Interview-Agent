document.getElementById("beginBtn").addEventListener("click", async () => {
  const role = document.getElementById("roleSelect").value;
  const language = document.getElementById("languageSelect").value;
  const errorMsg = document.getElementById("errorMsg");
  const beginBtn = document.getElementById("beginBtn");

  errorMsg.textContent = "";
  beginBtn.disabled = true;
  beginBtn.textContent = "Starting...";

  try {
    const response = await fetch(`${API_BASE_URL}/start`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json"
      },
      body: JSON.stringify({ role, language })
    });

    if (!response.ok) {
      throw new Error("Could not start interview. Please try again.");
    }

    const data = await response.json();

    // Store everything the interview screen will need
    sessionStorage.setItem("sessionId", data.sessionId);
    sessionStorage.setItem("role", role);
    sessionStorage.setItem("language", language);
    sessionStorage.setItem("currentQuestion", data.question);
    sessionStorage.setItem("currentCategory", data.category);
    sessionStorage.setItem("currentDifficulty", data.difficulty);

    window.location.href = "interview.html";

  } catch (err) {
    errorMsg.textContent = err.message;
    beginBtn.disabled = false;
    beginBtn.textContent = "Begin Interview";
  }
});