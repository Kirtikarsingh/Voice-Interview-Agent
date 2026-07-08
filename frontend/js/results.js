const finalFeedbackRaw = sessionStorage.getItem("finalFeedback");

if (!finalFeedbackRaw) {
  window.location.href = "setup.html";
}

const feedback = JSON.parse(finalFeedbackRaw);

document.getElementById("scoreValue").textContent = feedback.overallScore ?? "--";
document.getElementById("summaryText").textContent = feedback.summary || "No summary available.";

const strengthsList = document.getElementById("strengthsList");
(feedback.strengths || []).forEach(item => {
  const li = document.createElement("li");
  li.textContent = item;
  strengthsList.appendChild(li);
});

const improveList = document.getElementById("improveList");
(feedback.areasToImprove || []).forEach(item => {
  const li = document.createElement("li");
  li.textContent = item;
  improveList.appendChild(li);
});

const breakdownContainer = document.getElementById("breakdownContainer");
const breakdown = feedback.questionBreakdown || [];

breakdown.forEach((item, index) => {
  const card = document.createElement("div");
  card.className = "breakdown-card";

  card.innerHTML = `
    <p class="breakdown-question">Q${index + 1}: ${item.question}</p>

    <p class="breakdown-label">Candidate's Answer</p>
    <p class="breakdown-answer">${item.candidateAnswer}</p>

    <p class="breakdown-label">Ideal Answer</p>
    <p class="breakdown-answer ideal">${item.idealAnswer}</p>

    <div class="score-row">
      <div class="score-box">
        <span class="score-box-label">Technical Accuracy</span>
        <span class="score-box-value">${item.technicalAccuracy}/100</span>
      </div>
      <div class="score-box">
        <span class="score-box-label">Completeness</span>
        <span class="score-box-value">${item.completeness}/100</span>
      </div>
      <div class="score-box">
        <span class="score-box-label">Clarity</span>
        <span class="score-box-value">${item.clarity}/100</span>
      </div>
    </div>

    <p class="breakdown-comment">${item.comment}</p>
  `;

  breakdownContainer.appendChild(card);
});

document.getElementById("restartBtn").addEventListener("click", () => {
  sessionStorage.clear();
  window.location.href = "index.html";
});