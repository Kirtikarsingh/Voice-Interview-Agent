const sessionId = sessionStorage.getItem("sessionId");
const language = sessionStorage.getItem("language") || "en";

if (!sessionId) {
  window.location.href = "setup.html";
}

let currentQuestion = sessionStorage.getItem("currentQuestion");
let currentCategory = sessionStorage.getItem("currentCategory");
let mediaRecorder;
let audioChunks = [];
let isRecording = false;
let questionCount = 1;

const questionText = document.getElementById("questionText");
const categoryTag = document.getElementById("categoryTag");
const hearBtn = document.getElementById("hearBtn");
const recordBtn = document.getElementById("recordBtn");
const recordingStatus = document.getElementById("recordingStatus");
const resultSection = document.getElementById("resultSection");
const transcribedText = document.getElementById("transcribedText");
const feedbackText = document.getElementById("feedbackText");
const nextBtn = document.getElementById("nextBtn");
const progressText = document.getElementById("progressText");
const errorMsg = document.getElementById("errorMsg");

function loadQuestion(question, category) {
  questionText.textContent = question;
  categoryTag.textContent = category || "";
  progressText.textContent = `Question ${questionCount}`;
  resultSection.style.display = "none";
  recordBtn.style.display = "inline-block";
  errorMsg.textContent = "";

  // Reset recording button back to default state
  recordBtn.textContent = "🎙️ Start Recording";
  recordBtn.classList.remove("recording");
  isRecording = false;
  recordingStatus.textContent = "";
}

loadQuestion(currentQuestion, currentCategory);

// Hear Question button -> calls /speak, plays audio
hearBtn.addEventListener("click", async () => {
  hearBtn.disabled = true;
  hearBtn.textContent = "Loading...";

  try {
    const response = await fetch(`${API_BASE_URL}/speak`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ text: questionText.textContent })
    });

    if (!response.ok) throw new Error("Could not play audio.");

    const audioBlob = await response.blob();
    const audioUrl = URL.createObjectURL(audioBlob);
    const audio = new Audio(audioUrl);
    audio.play();

  } catch (err) {
    errorMsg.textContent = err.message;
  } finally {
    hearBtn.disabled = false;
    hearBtn.textContent = "🔊 Hear Question";
  }
});

// Record button -> toggle recording
recordBtn.addEventListener("click", async () => {
  if (!isRecording) {
    try {
      const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      mediaRecorder = new MediaRecorder(stream);
      audioChunks = [];

      mediaRecorder.ondataavailable = (e) => audioChunks.push(e.data);
      mediaRecorder.onstop = handleRecordingStop;

      mediaRecorder.start();
      isRecording = true;
      recordBtn.textContent = "⏹️ Stop Recording";
      recordBtn.classList.add("recording");
      recordingStatus.textContent = "Recording... click again to stop.";
    } catch (err) {
      errorMsg.textContent = "Microphone access denied or unavailable.";
    }
  } else {
    mediaRecorder.stop();
    isRecording = false;
    recordBtn.classList.remove("recording");
    recordingStatus.textContent = "Processing your answer...";
  }
});

async function handleRecordingStop() {
  const audioBlob = new Blob(audioChunks, { type: "audio/webm" });
  recordBtn.disabled = true;

  try {
    const formData = new FormData();
    formData.append("audio", audioBlob, "answer.webm");

    const url = `${API_BASE_URL}/voice-answer?sessionId=${encodeURIComponent(sessionId)}&language=${encodeURIComponent(language)}`;

    const response = await fetch(url, {
      method: "POST",
      body: formData
    });

    if (!response.ok) throw new Error("Could not process your answer.");

    const data = await response.json();

    transcribedText.textContent = data.transcribedAnswer;
    feedbackText.textContent = data.feedback;
    resultSection.style.display = "block";
    recordBtn.style.display = "none";
    recordingStatus.textContent = "";

    if (data.completed) {
      sessionStorage.setItem("finalFeedback", JSON.stringify(data.finalFeedback));
      nextBtn.textContent = "See Results →";
      nextBtn.onclick = () => window.location.href = "results.html";
    } else {
      const nextQuestion = data.nextQuestion;
      const nextCategory = data.isFollowUp ? "Follow-up" : currentCategory;

      nextBtn.textContent = "Next Question →";
      nextBtn.onclick = () => {
        questionCount++;
        currentQuestion = nextQuestion;
        currentCategory = nextCategory;
        loadQuestion(currentQuestion, currentCategory);
      };
    }

  } catch (err) {
    errorMsg.textContent = err.message;
    recordBtn.style.display = "inline-block";
    recordBtn.textContent = "🎙️ Start Recording";
    recordingStatus.textContent = "";
  } finally {
    recordBtn.disabled = false;
  }
}